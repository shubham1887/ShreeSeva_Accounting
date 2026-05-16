using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Requests;
using Medital_Application.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;

namespace Medital_Application.ViewModels;

public partial class SaleEntryViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly IProductRepository _productRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IStockRepository _stockRepo;

    [ObservableProperty] private string _voucherNo = "(Auto)";
    [ObservableProperty] private DateTime _voucherDate = DateTime.Today;
    [ObservableProperty] private int _selectedAccountId;
    [ObservableProperty] private string _accountSearchText = string.Empty;
    [ObservableProperty] private int? _patientId;
    [ObservableProperty] private int? _doctorId;
    [ObservableProperty] private string _paymentMode = "CASH";
    [ObservableProperty] private string? _chequeNo;
    [ObservableProperty] private DateTime? _chequeDate;
    [ObservableProperty] private string? _upiRef;
    [ObservableProperty] private decimal _cashDiscountPer;
    [ObservableProperty] private string? _narration;
    [ObservableProperty] private bool _isInterState;

    // Totals
    [ObservableProperty] private decimal _grossAmount;
    [ObservableProperty] private decimal _itemDiscAmount;
    [ObservableProperty] private decimal _cashDiscAmount;
    [ObservableProperty] private decimal _totalSGST;
    [ObservableProperty] private decimal _totalCGST;
    [ObservableProperty] private decimal _totalIGST;
    [ObservableProperty] private decimal _roundOff;
    [ObservableProperty] private decimal _netAmount;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _productSearchText = string.Empty;

    // Search results
    public ObservableCollection<Account> AccountSearchResults { get; } = new();
    public ObservableCollection<Product> ProductSearchResults { get; } = new();
    public ObservableCollection<SaleItemViewModel> Items { get; } = new();

    public int CurrentUserId { get; set; }
    public string FinancialYear { get; set; } = "2425";

    public SaleEntryViewModel(
        ISaleService saleService,
        IProductRepository productRepo,
        IAccountRepository accountRepo,
        IStockRepository stockRepo)
    {
        _saleService = saleService;
        _productRepo = productRepo;
        _accountRepo = accountRepo;
        _stockRepo = stockRepo;
        Items.CollectionChanged += (_, _) => RecalculateTotals();
    }

    [RelayCommand]
    private async Task SearchAccountAsync()
    {
        if (AccountSearchText.Length < 2) return;
        var results = await _accountRepo.SearchAsync(AccountSearchText);
        AccountSearchResults.Clear();
        foreach (var a in results) AccountSearchResults.Add(a);
    }

    [RelayCommand]
    private async Task SearchProductAsync()
    {
        if (ProductSearchText.Length < 2) return;
        var results = await _productRepo.SearchAsync(new Requests.SearchProductRequest
        {
            SearchTerm = ProductSearchText,
            OnlyInStock = true,
            PageSize = 20
        });
        ProductSearchResults.Clear();
        foreach (var p in results) ProductSearchResults.Add(p);
    }

    [RelayCommand]
    private async Task AddProductAsync(Product product)
    {
        var batches = await _stockRepo.GetAvailableBatchesAsync(product.Id);
        if (!batches.Any())
        {
            StatusMessage = $"No stock available for {product.ProductName}";
            return;
        }
        var firstBatch = batches[0];
        var item = new SaleItemViewModel
        {
            SrNo = Items.Count + 1,
            ProductId = product.Id,
            ProductName = product.ProductName,
            BatchNo = firstBatch.BatchNo,
            ExpiryMY = firstBatch.ExpiryMY,
            StockId = firstBatch.Id,
            Quantity = 1,
            SaleRate = firstBatch.SaleRate > 0 ? firstBatch.SaleRate : product.DefaultSaleRate,
            Mrp = firstBatch.MRP > 0 ? firstBatch.MRP : product.DefaultMRP,
            SgstRate = product.SGSTRate,
            CgstRate = product.CGSTRate,
            IgstRate = product.IGSTRate,
            PurchaseRate = firstBatch.ActualRate,
            HsnCode = product.HSNCode,
            IsInterState = IsInterState,
        };
        item.PropertyChanged += (_, _) => RecalculateTotals();
        Items.Add(item);
        RecalculateTotals();
    }

    [RelayCommand]
    private void RemoveItem(SaleItemViewModel item)
    {
        Items.Remove(item);
        var i = 1;
        foreach (var it in Items) it.SrNo = i++;
        RecalculateTotals();
    }

    partial void OnCashDiscountPerChanged(decimal value) => RecalculateTotals();
    partial void OnIsInterStateChanged(bool value)
    {
        foreach (var item in Items) item.IsInterState = value;
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        GrossAmount = Items.Sum(i => i.LineTotal);
        ItemDiscAmount = Items.Sum(i => i.ItemDiscAmt);
        TotalSGST = Items.Sum(i => i.SgstAmount);
        TotalCGST = Items.Sum(i => i.CgstAmount);
        TotalIGST = Items.Sum(i => i.IgstAmount);
        CashDiscAmount = Math.Round(GrossAmount * CashDiscountPer / 100, 2);
        var net = GrossAmount - CashDiscAmount;
        RoundOff = Math.Round(Math.Round(net) - net, 2);
        NetAmount = Math.Round(net + RoundOff, 2);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedAccountId == 0) { StatusMessage = "Please select customer account."; return; }
        if (!Items.Any()) { StatusMessage = "No items in the sale."; return; }

        IsSaving = true;
        StatusMessage = "Saving sale...";
        try
        {
            var request = new CreateSaleRequest
            {
                AccountId = SelectedAccountId,
                PatientId = PatientId,
                DoctorId = DoctorId,
                VoucherDate = VoucherDate,
                CashDiscountPer = CashDiscountPer,
                PaymentMode = PaymentMode,
                ChequeNo = ChequeNo,
                ChequeDate = ChequeDate,
                UPIRef = UpiRef,
                Narration = Narration,
                IsInterState = IsInterState,
                FinancialYear = FinancialYear,
                CreatedByUserId = CurrentUserId,
                SaleItems = Items.Select(i => new SaleItemRequest
                {
                    ProductId = i.ProductId,
                    BatchNo = i.BatchNo,
                    ExpiryMY = i.ExpiryMY,
                    StockId = i.StockId,
                    Quantity = i.Quantity,
                    FreeQty = i.FreeQty,
                    SaleRate = i.SaleRate,
                    MRP = i.Mrp,
                    ItemDiscPer = i.ItemDiscPer,
                    ItemDiscAmt = i.ItemDiscAmt,
                    SGSTRate = i.SgstRate,
                    CGSTRate = i.CgstRate,
                    IGSTRate = i.IgstRate,
                    PurchaseRate = i.PurchaseRate,
                    HSNCode = i.HsnCode,
                    ProductName = i.ProductName,
                }).ToList(),
            };

            var result = await _saleService.CreateSaleAsync(request);
            if (result.Success)
            {
                StatusMessage = $"Sale {result.Data?.VoucherNo} saved successfully!";
                ClearForm();
            }
            else
            {
                StatusMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void ClearForm()
    {
        Items.Clear();
        AccountSearchText = string.Empty;
        SelectedAccountId = 0;
        PatientId = null;
        DoctorId = null;
        CashDiscountPer = 0;
        PaymentMode = "CASH";
        ChequeNo = null;
        ChequDate = null;
        UpiRef = null;
        Narration = null;
        VoucherDate = DateTime.Today;
        StatusMessage = string.Empty;
    }

    // Disambiguate property with backing field
    private DateTime? _chequDate;
    public DateTime? ChequDate
    {
        get => _chequDate;
        set => SetProperty(ref _chequDate, value);
    }
}
