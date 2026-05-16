using Medital_Application.Helpers;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Requests;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Medital_Application.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepo;
    private readonly IStockRepository _stockRepo;
    private readonly IProductRepository _productRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IUserService _userService;
    private readonly IWhatsAppService _waService;
    private readonly IConfiguration _config;
    private readonly ILogger<SaleService> _logger;

    public SaleService(
        ISaleRepository saleRepo,
        IStockRepository stockRepo,
        IProductRepository productRepo,
        IAccountRepository accountRepo,
        IUserService userService,
        IWhatsAppService waService,
        IConfiguration config,
        ILogger<SaleService> logger)
    {
        _saleRepo = saleRepo;
        _stockRepo = stockRepo;
        _productRepo = productRepo;
        _accountRepo = accountRepo;
        _userService = userService;
        _waService = waService;
        _config = config;
        _logger = logger;
    }

    public async Task<ApiResponse<SaleResponse>> CreateSaleAsync(CreateSaleRequest request)
    {
        // 1. Permission check
        if (!await _userService.CheckPermissionAsync(request.CreatedByUserId, "CanSale"))
            return ApiResponse<SaleResponse>.Fail("You do not have permission to create sales.");

        if (request.SaleItems == null || !request.SaleItems.Any())
            return ApiResponse<SaleResponse>.Fail("No sale items provided.");

        // 2. Get account
        var account = await _accountRepo.GetByIdAsync(request.AccountId);
        if (account == null)
            return ApiResponse<SaleResponse>.Fail("Account not found.");

        // 3. Get voucher number
        var fy = request.FinancialYear.TrimToEmpty();
        var voucherNo = await _saleRepo.GetNextVoucherNoAsync(fy);

        // 4. Process each item, validate stock, calculate GST
        var details = new List<SaleDetail>();
        var stockDeductions = new List<(int StockId, decimal Qty)>();

        foreach (var item in request.SaleItems)
        {
            if (item.Quantity <= 0)
                return ApiResponse<SaleResponse>.Fail($"Invalid quantity for product ID {item.ProductId}");

            // Validate stock availability (FIFO)
            var batches = await _stockRepo.GetAvailableBatchesAsync(item.ProductId);
            decimal remaining = item.Quantity;
            var stocksForItem = new List<(Stock stock, decimal deductQty)>();

            if (item.StockId.HasValue)
            {
                // Specific batch requested
                var specific = batches.FirstOrDefault(b => b.Id == item.StockId.Value);
                if (specific == null || specific.CurrentQty < item.Quantity)
                    return ApiResponse<SaleResponse>.Fail($"Insufficient stock for {item.ProductName} batch {item.BatchNo}");
                stocksForItem.Add((specific, item.Quantity));
            }
            else
            {
                // FIFO auto-selection
                foreach (var batch in batches)
                {
                    if (remaining <= 0) break;
                    var take = Math.Min(remaining, batch.CurrentQty);
                    stocksForItem.Add((batch, take));
                    remaining -= take;
                }
                if (remaining > 0)
                    return ApiResponse<SaleResponse>.Fail($"Insufficient stock for product ID {item.ProductId}. Need {item.Quantity} more units.");
            }

            // Calculate GST per line
            var taxableAmt = (item.Quantity * item.SaleRate) - item.ItemDiscAmt;
            var gstCalc = GSTCalculator.Calculate(taxableAmt, item.SGSTRate, item.CGSTRate, item.IGSTRate, request.IsInterState);

            // Use first batch for detail (or split if needed - simplified: use primary batch)
            var primaryStock = stocksForItem[0].stock;

            var detail = new SaleDetail
            {
                ProductId = item.ProductId,
                BatchNo = primaryStock.BatchNo,
                ExpiryMY = primaryStock.ExpiryMY,
                ExpiryDate = primaryStock.ExpiryDate,
                Quantity = item.Quantity,
                FreeQuantity = item.FreeQty,
                SaleRate = item.SaleRate,
                MRP = item.MRP,
                ItemDiscPer = item.ItemDiscPer,
                ItemDiscAmt = item.ItemDiscAmt,
                SGSTRate = item.SGSTRate,
                CGSTRate = item.CGSTRate,
                IGSTRate = item.IGSTRate,
                SGSTAmount = gstCalc.SGSTAmount,
                CGSTAmount = gstCalc.CGSTAmount,
                IGSTAmount = gstCalc.IGSTAmount,
                TaxableAmount = taxableAmt,
                LineTotal = taxableAmt + gstCalc.SGSTAmount + gstCalc.CGSTAmount + gstCalc.IGSTAmount,
                PurchaseRate = primaryStock.ActualRate,
                Profit = taxableAmt - (item.Quantity * primaryStock.ActualRate),
                StockKey = primaryStock.StockKey,
                StockId = primaryStock.Id,
            };
            details.Add(detail);

            foreach (var (stock, qty) in stocksForItem)
                stockDeductions.Add((stock.Id, qty));
        }

        // 5. Calculate totals
        var grossAmount = details.Sum(d => d.LineTotal);
        var itemDiscTotal = details.Sum(d => d.ItemDiscAmt);
        var cdAmount = Math.Round(grossAmount * request.CashDiscountPer / 100, 2);
        var afterCd = grossAmount - cdAmount;
        var totalSGST = details.Sum(d => d.SGSTAmount);
        var totalCGST = details.Sum(d => d.CGSTAmount);
        var totalIGST = details.Sum(d => d.IGSTAmount);
        var roundOff = Math.Round(afterCd) - afterCd;
        var netAmount = afterCd + roundOff;

        var master = new SaleMaster
        {
            VoucherNo = voucherNo,
            VoucherDate = request.VoucherDate,
            TransactionType = "SA",
            AccountId = request.AccountId,
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            GrossAmount = grossAmount,
            ItemDiscAmount = itemDiscTotal,
            CashDiscPer = request.CashDiscountPer,
            CashDiscAmount = cdAmount,
            TotalSGST = totalSGST,
            TotalCGST = totalCGST,
            TotalIGST = totalIGST,
            RoundOff = Math.Round(roundOff, 2),
            NetAmount = Math.Round(netAmount, 2),
            PaymentMode = request.PaymentMode,
            ChequeNo = request.ChequeNo,
            ChequeDate = request.ChequeDate,
            UPIRef = request.UPIRef,
            Narration = request.Narration,
            FinancialYear = fy,
            IsInterState = request.IsInterState,
        };

        // 6. Save to DB
        var saleId = await _saleRepo.CreateAsync(master, details);
        master.Id = saleId;

        // 7. Deduct stock FIFO
        foreach (var (stockId, qty) in stockDeductions)
        {
            await _stockRepo.DeductSoldQtyAsync(stockId, qty);
        }

        // 8. Update product current qty
        foreach (var item in request.SaleItems)
        {
            var currentQty = await _stockRepo.GetCurrentQtyAsync(item.ProductId);
            await _productRepo.UpdateStockQtyAsync(item.ProductId, currentQty);
        }

        // 9. Build response
        var itemResponses = details.Select((d, i) => new SaleItemResponse
        {
            ProductName = request.SaleItems[i].ProductName ?? "",
            BatchNo = d.BatchNo,
            ExpiryMY = d.ExpiryMY,
            Quantity = d.Quantity,
            SaleRate = d.SaleRate,
            MRP = d.MRP,
            ItemDiscAmt = d.ItemDiscAmt,
            TaxableAmount = d.TaxableAmount,
            SGSTAmount = d.SGSTAmount,
            CGSTAmount = d.CGSTAmount,
            IGSTAmount = d.IGSTAmount,
            LineTotal = d.LineTotal,
            HSNCode = request.SaleItems[i].HSNCode,
        }).ToList();

        var saleResponse = new SaleResponse
        {
            SaleId = saleId,
            VoucherNo = voucherNo,
            VoucherDate = request.VoucherDate,
            AccountName = account.AccountName,
            GrossAmount = grossAmount,
            ItemDiscAmount = itemDiscTotal,
            CashDiscAmount = cdAmount,
            TotalSGST = totalSGST,
            TotalCGST = totalCGST,
            TotalIGST = totalIGST,
            RoundOff = Math.Round(roundOff, 2),
            NetAmount = Math.Round(netAmount, 2),
            PaymentMode = request.PaymentMode,
            Items = itemResponses,
            AmountInWords = NumberToWords.Convert((long)Math.Round(netAmount)),
        };

        // 10. WhatsApp bill
        if (_waService.IsEnabled && !string.IsNullOrEmpty(account.Mobile)
            && _config["WhatsApp:SendBillOnSale"] == "true")
        {
            try
            {
                await _waService.SendBillAsync(saleResponse, account.Mobile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WhatsApp send failed for sale {VoucherNo}", voucherNo);
            }
        }

        return ApiResponse<SaleResponse>.Ok(saleResponse, $"Sale {voucherNo} saved successfully.");
    }

    public Task<SaleMaster?> GetByIdAsync(int id) => _saleRepo.GetByIdAsync(id);

    public Task<List<SaleMaster>> GetByDateRangeAsync(DateTime from, DateTime to, int? accountId = null) =>
        _saleRepo.GetByDateRangeAsync(from, to, accountId);

    public async Task<ApiResponse> CancelSaleAsync(int id, int userId)
    {
        if (!await _userService.CheckPermissionAsync(userId, "CanSaleDelete"))
            return ApiResponse.Fail("Permission denied.");
        var ok = await _saleRepo.CancelAsync(id);
        return ok ? ApiResponse.Ok("Sale cancelled.") : ApiResponse.Fail("Cancel failed.");
    }
}

internal static class StringExtensions
{
    public static string TrimToEmpty(this string? s) => s?.Trim() ?? string.Empty;
}
