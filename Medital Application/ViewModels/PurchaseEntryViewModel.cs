using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Requests;
using Medital_Application.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class PurchaseEntryViewModel : ObservableObject
{
    private readonly IPurchaseService _purchaseService;
    private readonly IProductRepository _productRepo;
    private readonly IAccountRepository _accountRepo;

    [ObservableProperty] private string _voucherNo = "(Auto)";
    [ObservableProperty] private DateTime _voucherDate = DateTime.Today;
    [ObservableProperty] private int _selectedAccountId;
    [ObservableProperty] private string _accountSearchText = string.Empty;
    [ObservableProperty] private string? _billNo;
    [ObservableProperty] private DateTime? _billDate;
    [ObservableProperty] private string? _challanNo;
    [ObservableProperty] private DateTime? _challanDate;
    [ObservableProperty] private decimal _specialDisc;
    [ObservableProperty] private decimal _freightAmount;
    [ObservableProperty] private bool _isInterState;
    [ObservableProperty] private string? _narration;
    [ObservableProperty] private string _productSearchText = string.Empty;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _statusMessage = string.Empty;

    // Totals
    [ObservableProperty] private decimal _grossAmount;
    [ObservableProperty] private decimal _itemDiscAmount;
    [ObservableProperty] private decimal _totalSGST;
    [ObservableProperty] private decimal _totalCGST;
    [ObservableProperty] private decimal _totalIGST;
    [ObservableProperty] private decimal _roundOff;
    [ObservableProperty] private decimal _netAmount;

    public ObservableCollection<Account> AccountSearchResults { get; } = new();
    public ObservableCollection<Product> ProductSearchResults { get; } = new();
    public ObservableCollection<PurchaseItemViewModel> Items { get; } = new();

    public int CurrentUserId { get; set; }
    public string FinancialYear { get; set; } = "2425";

    public PurchaseEntryViewModel(IPurchaseService purchaseService, IProductRepository productRepo, IAccountRepository accountRepo)
    {
        _purchaseService = purchaseService;
        _productRepo = productRepo;
        _accountRepo = accountRepo;
        Items.CollectionChanged += (_, _) => RecalculateTotals();
    }

    [RelayCommand]
    private async Task SearchAccountAsync()
    {
        var results = await _accountRepo.GetDistributorsAsync();
        AccountSearchResults.Clear();
        foreach (var a in results.Where(a => a.AccountName.Contains(AccountSearchText, StringComparison.OrdinalIgnoreCase)))
            AccountSearchResults.Add(a);
    }

    [RelayCommand]
    private async Task SearchProductAsync()
    {
        if (ProductSearchText.Length < 2) return;
        var results = await _productRepo.SearchAsync(new SearchProductRequest { SearchTerm = ProductSearchText, PageSize = 20 });
        ProductSearchResults.Clear();
        foreach (var p in results) ProductSearchResults.Add(p);
    }

    [RelayCommand]
    private void AddProduct(Product product)
    {
        var item = new PurchaseItemViewModel
        {
            SrNo = Items.Count + 1,
            ProductId = product.Id,
            ProductName = product.ProductName,
            Quantity = 1,
            ActualRate = product.LastPurchaseRate,
            Mrp = product.DefaultMRP,
            SaleRate = product.DefaultSaleRate,
            SgstRate = product.SGSTRate,
            CgstRate = product.CGSTRate,
            IgstRate = product.IGSTRate,
            HsnCode = product.HSNCode,
            IsInterState = IsInterState,
        };
        item.PropertyChanged += (_, _) => RecalculateTotals();
        Items.Add(item);
        RecalculateTotals();
    }

    [RelayCommand]
    private void RemoveItem(PurchaseItemViewModel item)
    {
        Items.Remove(item);
        var i = 1; foreach (var it in Items) it.SrNo = i++;
        RecalculateTotals();
    }

    partial void OnSpecialDiscChanged(decimal value) => RecalculateTotals();
    partial void OnFreightAmountChanged(decimal value) => RecalculateTotals();

    private void RecalculateTotals()
    {
        GrossAmount = Items.Sum(i => i.LineTotal);
        ItemDiscAmount = Items.Sum(i => i.ItemDiscAmt);
        TotalSGST = Items.Sum(i => i.SgstAmount);
        TotalCGST = Items.Sum(i => i.CgstAmount);
        TotalIGST = Items.Sum(i => i.IgstAmount);
        var net = GrossAmount - SpecialDisc + FreightAmount;
        RoundOff = Math.Round(Math.Round(net) - net, 2);
        NetAmount = Math.Round(net + RoundOff, 2);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedAccountId == 0) { StatusMessage = "Please select supplier."; return; }
        if (!Items.Any()) { StatusMessage = "No items added."; return; }

        IsSaving = true;
        try
        {
            var result = await _purchaseService.CreatePurchaseAsync(new CreatePurchaseRequest
            {
                AccountId = SelectedAccountId,
                BillNo = BillNo,
                BillDate = BillDate,
                ChallanNo = ChallanNo,
                ChallanDate = ChallanDate,
                VoucherDate = VoucherDate,
                SpecialDisc = SpecialDisc,
                FreightAmount = FreightAmount,
                IsInterState = IsInterState,
                Narration = Narration,
                FinancialYear = FinancialYear,
                CreatedByUserId = CurrentUserId,
                PurchaseItems = Items.Select(i => new PurchaseItemRequest
                {
                    ProductId = i.ProductId,
                    BatchNo = i.BatchNo,
                    ExpiryMY = i.ExpiryMY,
                    Quantity = i.Quantity,
                    FreeQuantity = i.FreeQuantity,
                    ActualRate = i.ActualRate,
                    MRP = i.Mrp,
                    SaleRate = i.SaleRate,
                    ItemDiscPer = i.ItemDiscPer,
                    SGSTRate = i.SgstRate,
                    CGSTRate = i.CgstRate,
                    IGSTRate = i.IgstRate,
                    HSNCode = i.HsnCode,
                    ProductName = i.ProductName,
                }).ToList(),
            });

            StatusMessage = result.Success ? $"Purchase {result.Data?.VoucherNo} saved!" : result.Message;
            if (result.Success) ClearForm();
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { IsSaving = false; }
    }

    [RelayCommand]
    private void ClearForm()
    {
        Items.Clear();
        AccountSearchText = string.Empty;
        SelectedAccountId = 0;
        BillNo = null; BillDate = null; ChallanNo = null; ChallanDate = null;
        SpecialDisc = 0; FreightAmount = 0; Narration = null;
        VoucherDate = DateTime.Today;
    }
}
