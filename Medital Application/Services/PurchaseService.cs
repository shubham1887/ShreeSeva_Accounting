using Medital_Application.Helpers;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Requests;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Medital_Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IStockRepository _stockRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUserService _userService;
    private readonly ILogger<PurchaseService> _logger;

    public PurchaseService(
        IPurchaseRepository purchaseRepo,
        IStockRepository stockRepo,
        IProductRepository productRepo,
        IUserService userService,
        ILogger<PurchaseService> logger)
    {
        _purchaseRepo = purchaseRepo;
        _stockRepo = stockRepo;
        _productRepo = productRepo;
        _userService = userService;
        _logger = logger;
    }

    public async Task<ApiResponse<PurchaseResponse>> CreatePurchaseAsync(CreatePurchaseRequest request)
    {
        if (!await _userService.CheckPermissionAsync(request.CreatedByUserId, "CanPurchase"))
            return ApiResponse<PurchaseResponse>.Fail("Permission denied.");

        if (request.PurchaseItems == null || !request.PurchaseItems.Any())
            return ApiResponse<PurchaseResponse>.Fail("No purchase items provided.");

        var fy = request.FinancialYear.TrimToEmpty();
        var voucherNo = await _purchaseRepo.GetNextVoucherNoAsync(fy);

        var details = new List<PurchaseDetail>();
        foreach (var item in request.PurchaseItems)
        {
            if (item.Quantity <= 0)
                return ApiResponse<PurchaseResponse>.Fail($"Invalid quantity for product {item.ProductName}");

            // Parse expiry: MM/YYYY -> last day of that month
            var expiryDate = ParseExpiryDate(item.ExpiryMY);
            var itemDiscAmt = Math.Round(item.Quantity * item.ActualRate * item.ItemDiscPer / 100, 2);
            var netRate = item.ActualRate - (item.ActualRate * item.ItemDiscPer / 100);
            var taxableAmt = item.Quantity * netRate;
            var gstCalc = GSTCalculator.Calculate(taxableAmt, item.SGSTRate, item.CGSTRate, item.IGSTRate, request.IsInterState);

            var stockKey = $"{item.ProductId}_{item.BatchNo}";

            var detail = new PurchaseDetail
            {
                ProductId = item.ProductId,
                BatchNo = item.BatchNo,
                ExpiryMY = item.ExpiryMY,
                ExpiryDate = expiryDate,
                Quantity = item.Quantity,
                FreeQuantity = item.FreeQuantity,
                ActualRate = item.ActualRate,
                NetRate = Math.Round(netRate, 4),
                MRP = item.MRP,
                SaleRate = item.SaleRate,
                ItemDiscPer = item.ItemDiscPer,
                ItemDiscAmt = itemDiscAmt,
                SGSTRate = item.SGSTRate,
                CGSTRate = item.CGSTRate,
                IGSTRate = item.IGSTRate,
                SGSTAmount = gstCalc.SGSTAmount,
                CGSTAmount = gstCalc.CGSTAmount,
                IGSTAmount = gstCalc.IGSTAmount,
                TaxableAmount = taxableAmt,
                LineTotal = taxableAmt + gstCalc.SGSTAmount + gstCalc.CGSTAmount + gstCalc.IGSTAmount,
                StockKey = stockKey,
            };
            details.Add(detail);
        }

        // Compute master totals
        var gross = details.Sum(d => d.LineTotal);
        var itemDiscTotal = details.Sum(d => d.ItemDiscAmt);
        var totalSGST = details.Sum(d => d.SGSTAmount);
        var totalCGST = details.Sum(d => d.CGSTAmount);
        var totalIGST = details.Sum(d => d.IGSTAmount);
        var net = gross - request.SpecialDisc + request.FreightAmount;
        var roundOff = Math.Round(net) - net;
        net += roundOff;

        var master = new PurchaseMaster
        {
            VoucherNo = voucherNo,
            VoucherDate = request.VoucherDate,
            BillNo = request.BillNo,
            BillDate = request.BillDate,
            ChallanNo = request.ChallanNo,
            ChallanDate = request.ChallanDate,
            AccountId = request.AccountId,
            GrossAmount = gross,
            ItemDiscAmount = itemDiscTotal,
            SpecialDisc = request.SpecialDisc,
            FreightAmount = request.FreightAmount,
            TotalSGST = totalSGST,
            TotalCGST = totalCGST,
            TotalIGST = totalIGST,
            RoundOff = Math.Round(roundOff, 2),
            NetAmount = Math.Round(net, 2),
            FinancialYear = fy,
            Narration = request.Narration,
        };

        var purchaseId = await _purchaseRepo.CreateAsync(master, details);

        // Update stock
        for (int i = 0; i < request.PurchaseItems.Count; i++)
        {
            var item = request.PurchaseItems[i];
            var detail = details[i];
            var stockKey = detail.StockKey;
            var existingStock = await _stockRepo.GetByKeyAsync(stockKey);

            if (existingStock != null)
            {
                existingStock.PurchasedQty += item.Quantity + item.FreeQuantity;
                existingStock.MRP = item.MRP;
                existingStock.SaleRate = item.SaleRate;
                existingStock.ActualRate = item.ActualRate;
                existingStock.NetRate = detail.NetRate;
                await _stockRepo.UpdateAsync(existingStock);
            }
            else
            {
                var newStock = new Stock
                {
                    ProductId = item.ProductId,
                    BatchNo = item.BatchNo,
                    ExpiryMY = item.ExpiryMY,
                    ExpiryDate = detail.ExpiryDate,
                    ActualRate = item.ActualRate,
                    NetRate = detail.NetRate,
                    MRP = item.MRP,
                    SaleRate = item.SaleRate,
                    PurchasedQty = item.Quantity + item.FreeQuantity,
                    StockKey = stockKey,
                };
                await _stockRepo.CreateAsync(newStock);
            }

            // Update product last purchase rate and sale rate
            var product = await _productRepo.GetByIdAsync(item.ProductId);
            if (product != null)
            {
                product.LastPurchaseRate = item.ActualRate;
                if (item.SaleRate > 0) product.DefaultSaleRate = item.SaleRate;
                if (item.MRP > 0) product.DefaultMRP = item.MRP;
                var currentQty = await _stockRepo.GetCurrentQtyAsync(item.ProductId);
                product.CurrentQty = currentQty;
                await _productRepo.UpdateAsync(product);
            }
        }

        var response = new PurchaseResponse
        {
            PurchaseId = purchaseId,
            VoucherNo = voucherNo,
            VoucherDate = request.VoucherDate,
            AccountName = "",
            BillNo = request.BillNo,
            GrossAmount = gross,
            ItemDiscAmount = itemDiscTotal,
            SpecialDisc = request.SpecialDisc,
            FreightAmount = request.FreightAmount,
            TotalSGST = totalSGST,
            TotalCGST = totalCGST,
            TotalIGST = totalIGST,
            RoundOff = Math.Round(roundOff, 2),
            NetAmount = Math.Round(net, 2),
            Items = details.Select((d, i) => new PurchaseItemResponse
            {
                ProductName = request.PurchaseItems[i].ProductName ?? "",
                BatchNo = d.BatchNo,
                ExpiryMY = d.ExpiryMY,
                Quantity = d.Quantity,
                ActualRate = d.ActualRate,
                MRP = d.MRP,
                SaleRate = d.SaleRate,
                ItemDiscAmt = d.ItemDiscAmt,
                TaxableAmount = d.TaxableAmount,
                SGSTAmount = d.SGSTAmount,
                CGSTAmount = d.CGSTAmount,
                IGSTAmount = d.IGSTAmount,
                LineTotal = d.LineTotal,
            }).ToList(),
        };

        return ApiResponse<PurchaseResponse>.Ok(response, $"Purchase {voucherNo} saved.");
    }

    public Task<PurchaseMaster?> GetByIdAsync(int id) => _purchaseRepo.GetByIdAsync(id);

    public Task<List<PurchaseMaster>> GetByDateRangeAsync(DateTime from, DateTime to, int? accountId = null) =>
        _purchaseRepo.GetByDateRangeAsync(from, to, accountId);

    public async Task<ApiResponse> CancelPurchaseAsync(int id, int userId)
    {
        if (!await _userService.CheckPermissionAsync(userId, "CanPurchaseDelete"))
            return ApiResponse.Fail("Permission denied.");
        var ok = await _purchaseRepo.CancelAsync(id);
        return ok ? ApiResponse.Ok("Purchase cancelled.") : ApiResponse.Fail("Cancel failed.");
    }

    private static string ParseExpiryDate(string expiryMY)
    {
        if (string.IsNullOrWhiteSpace(expiryMY)) return DateTime.Today.AddYears(1).ToString("yyyy-MM-dd");
        var parts = expiryMY.Split('/');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var month) &&
            int.TryParse(parts[1], out var year))
        {
            var lastDay = DateTime.DaysInMonth(year, month);
            return new DateTime(year, month, lastDay).ToString("yyyy-MM-dd");
        }
        return DateTime.Today.AddYears(1).ToString("yyyy-MM-dd");
    }
}
