using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Requests;
using Medital_Application.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;

namespace Medital_Application.ViewModels;

// ─── Bill Slot state carrier ───────────────────────────────────────────────
public class BillSlotState
{
    public int SlotNumber { get; init; }
    public List<CounterSaleItemVM> Items { get; set; } = new();
    public string PaymentMode { get; set; } = "CASH";
    public string CustomerMobile { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }

    public int ItemCount => Items.Count;
    public string Label => $"BILL {SlotNumber}";
    public string BadgeText => ItemCount > 0 ? $"{ItemCount} items · ₹{NetAmount:N0}" : "Empty";
}

// ─── Main ViewModel ────────────────────────────────────────────────────────
public partial class CounterSaleViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly ISaleRepository _saleRepo;
    private readonly IProductRepository _productRepo;
    private readonly IStockRepository _stockRepo;
    private readonly ISettingsService _settingsService;

    // ── Header ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string _voucherNo = "(Auto)";
    [ObservableProperty] private DateTime _voucherDate = DateTime.Today;
    [ObservableProperty] private int _activeBillSlot = 1;
    [ObservableProperty] private string _paymentMode = "CASH";
    [ObservableProperty] private string _customerMobile = string.Empty;
    [ObservableProperty] private string _customerName = string.Empty;

    // ── Totals ──────────────────────────────────────────────────────────────
    [ObservableProperty] private decimal _grossAmount;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _taxableAmount;
    [ObservableProperty] private decimal _totalGst;
    [ObservableProperty] private decimal _totalSgst;
    [ObservableProperty] private decimal _totalCgst;
    [ObservableProperty] private decimal _totalIgst;
    [ObservableProperty] private decimal _roundOff;
    [ObservableProperty] private decimal _netAmount;

    // ── Status / Search ─────────────────────────────────────────────────────
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private string _productSearchText = string.Empty;
    [ObservableProperty] private bool _showSearchDropdown;
    [ObservableProperty] private string _lastBillNo = string.Empty;
    [ObservableProperty] private decimal _lastBillAmt;
    [ObservableProperty] private decimal _adjustmentAmount;
    [ObservableProperty] private bool _isInterState;

    // ── Collections ─────────────────────────────────────────────────────────
    public ObservableCollection<CounterSaleItemVM> Items { get; } = new();
    public ObservableCollection<Stock> ProductSearchResults { get; } = new();
    public ObservableCollection<BillSlotState> BillSlots { get; } = new();

    // ── Injected state ──────────────────────────────────────────────────────
    public int CurrentUserId { get; set; }
    public string FinancialYear { get; set; } = "2526";

    // ── Slot storage (1-5) ──────────────────────────────────────────────────
    private readonly Dictionary<int, BillSlotState> _slotData = new();

    public CounterSaleViewModel(
        ISaleService saleService,
        ISaleRepository saleRepo,
        IProductRepository productRepo,
        IStockRepository stockRepo,
        ISettingsService settingsService)
    {
        _saleService = saleService;
        _saleRepo = saleRepo;
        _productRepo = productRepo;
        _stockRepo = stockRepo;
        _settingsService = settingsService;

        // Initialise 5 slots
        for (int i = 1; i <= 5; i++)
        {
            var slot = new BillSlotState { SlotNumber = i };
            _slotData[i] = slot;
            BillSlots.Add(slot);
        }

        Items.CollectionChanged += (_, _) => RecalculateTotals();
    }

    // ── Initialise ──────────────────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        try
        {
            var fy = await _settingsService.GetFinancialYearAsync();
            if (!string.IsNullOrWhiteSpace(fy)) FinancialYear = fy;
            VoucherNo = await _saleRepo.GetNextVoucherNoAsync(FinancialYear);
        }
        catch { /* non-fatal */ }
    }

    // ── Product Search ──────────────────────────────────────────────────────
    [RelayCommand]
    public async Task SearchProductAsync()
    {
        if (ProductSearchText.Length < 2)
        {
            ProductSearchResults.Clear();
            ShowSearchDropdown = false;
            return;
        }

        IsSearching = true;
        try
        {
            // Search by name/barcode - get in-stock items via product search then enrich with batch
            var products = await _productRepo.SearchAsync(new SearchProductRequest
            {
                SearchTerm = ProductSearchText,
                OnlyInStock = true,
                PageSize = 30
            });

            ProductSearchResults.Clear();
            foreach (var p in products)
            {
                // Fetch best batch and create a pseudo-stock for display
                var batches = await _stockRepo.GetAvailableBatchesAsync(p.Id);
                if (batches.Any())
                {
                    var best = batches[0];
                    // Augment batch display with product name
                    best.ProductName = p.ProductName;
                    ProductSearchResults.Add(best);
                }
            }
            ShowSearchDropdown = ProductSearchResults.Any();
        }
        finally
        {
            IsSearching = false;
        }
    }

    // ── Add Item ────────────────────────────────────────────────────────────
    [RelayCommand]
    public async Task AddItemAsync(Stock stock)
    {
        ShowSearchDropdown = false;
        ProductSearchText = string.Empty;

        // Check if same stock already in list - just increment qty
        var existing = Items.FirstOrDefault(i => i.StockId == stock.Id);
        if (existing != null)
        {
            existing.Quantity += 1;
            StatusMessage = $"Qty updated: {existing.ProductName}";
            return;
        }

        // Fetch full product details for GST / HSN
        var products = await _productRepo.SearchAsync(new SearchProductRequest
        {
            SearchTerm = stock.ProductName,
            PageSize = 1
        });
        var product = products.FirstOrDefault(p => p.Id == stock.ProductId);

        var item = new CounterSaleItemVM
        {
            SrNo = Items.Count + 1,
            ProductId = stock.ProductId,
            ProductName = stock.ProductName,
            BatchNo = stock.BatchNo,
            ExpiryMY = stock.ExpiryMY,
            StockId = stock.Id,
            Quantity = 1,
            Mrp = stock.MRP > 0 ? stock.MRP : (product?.DefaultMRP ?? 0),
            SaleRate = stock.SaleRate > 0 ? stock.SaleRate : (product?.DefaultSaleRate ?? 0),
            SgstRate = stock.SGSTRate > 0 ? stock.SGSTRate : (product?.SGSTRate ?? 0),
            CgstRate = stock.CGSTRate > 0 ? stock.CGSTRate : (product?.CGSTRate ?? 0),
            IgstRate = stock.IGSTRate > 0 ? stock.IGSTRate : (product?.IGSTRate ?? 0),
            PurchaseRate = stock.ActualRate,
            HsnCode = product?.HSNCode,
            IsInterState = IsInterState,
        };
        item.PropertyChanged += (_, _) => RecalculateTotals();
        Items.Add(item);
        RecalculateTotals();
        StatusMessage = $"Added: {item.ProductName}";
    }

    // ── Remove Item ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void RemoveItem(CounterSaleItemVM item)
    {
        Items.Remove(item);
        int i = 1;
        foreach (var it in Items) it.SrNo = i++;
        RecalculateTotals();
    }

    // ── Bill Slot switching ─────────────────────────────────────────────────
    [RelayCommand]
    private void SwitchBillSlot(int slotNumber)
    {
        if (slotNumber == ActiveBillSlot) return;

        // Persist current bill to slot
        SaveCurrentToSlot(ActiveBillSlot);

        // Load target slot
        ActiveBillSlot = slotNumber;
        LoadSlot(slotNumber);
        UpdateSlotBadges();
    }

    private void SaveCurrentToSlot(int slot)
    {
        var state = _slotData[slot];
        state.Items = Items.ToList();
        state.PaymentMode = PaymentMode;
        state.CustomerMobile = CustomerMobile;
        state.CustomerName = CustomerName;
        state.NetAmount = NetAmount;
    }

    private void LoadSlot(int slot)
    {
        var state = _slotData[slot];
        Items.Clear();
        foreach (var it in state.Items)
        {
            it.PropertyChanged += (_, _) => RecalculateTotals();
            Items.Add(it);
        }
        PaymentMode = state.PaymentMode;
        CustomerMobile = state.CustomerMobile;
        CustomerName = state.CustomerName;
        RecalculateTotals();
    }

    private void UpdateSlotBadges()
    {
        // Trigger UI refresh for badges by replacing items in BillSlots
        BillSlots.Clear();
        foreach (var s in _slotData.Values.OrderBy(x => x.SlotNumber))
            BillSlots.Add(s);
    }

    // ── Recalculate ─────────────────────────────────────────────────────────
    partial void OnIsInterStateChanged(bool value)
    {
        foreach (var item in Items) item.IsInterState = value;
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        GrossAmount = Items.Sum(i => i.LineTotal);
        DiscountAmount = Items.Sum(i => i.ItemDiscAmt);
        TaxableAmount = Items.Sum(i => i.TaxableAmount);
        TotalSgst = Items.Sum(i => i.SgstAmount);
        TotalCgst = Items.Sum(i => i.CgstAmount);
        TotalIgst = Items.Sum(i => i.IgstAmount);
        TotalGst = TotalSgst + TotalCgst + TotalIgst;
        var net = GrossAmount - AdjustmentAmount;
        RoundOff = Math.Round(Math.Round(net) - net, 2);
        NetAmount = Math.Round(net + RoundOff, 2);
    }

    partial void OnAdjustmentAmountChanged(decimal value) => RecalculateTotals();

    // ── Save (F10) ──────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Items.Any())
        {
            StatusMessage = "No items in the bill.";
            return;
        }

        IsSaving = true;
        StatusMessage = "Saving bill...";
        try
        {
            var request = new CreateSaleRequest
            {
                AccountId = 0, // Counter sale - walk-in customer
                VoucherDate = VoucherDate,
                PaymentMode = PaymentMode,
                Narration = string.IsNullOrWhiteSpace(CustomerName) ? "Counter Sale" : $"Counter Sale - {CustomerName}",
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
                    FreeQty = 0,
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
                LastBillNo = result.Data?.VoucherNo ?? string.Empty;
                LastBillAmt = NetAmount;
                StatusMessage = $"Bill {LastBillNo} saved! ₹{LastBillAmt:N2}";

                // Clear the current slot and refresh voucher
                ClearCurrentSlot();
                VoucherNo = await _saleRepo.GetNextVoucherNoAsync(FinancialYear);
            }
            else
            {
                StatusMessage = result.Message ?? "Save failed.";
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

    // ── Print (F12) ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void Print()
    {
        if (string.IsNullOrEmpty(LastBillNo))
        {
            StatusMessage = "Save the bill first before printing.";
            return;
        }
        StatusMessage = $"Printing bill {LastBillNo}...";
        // PrintService integration point - called from code-behind after Save
    }

    // ── WhatsApp ─────────────────────────────────────────────────────────────
    [RelayCommand]
    private void SendWhatsApp()
    {
        if (string.IsNullOrWhiteSpace(CustomerMobile))
        {
            StatusMessage = "Enter customer mobile number first.";
            return;
        }
        var formatted = Helpers.WhatsAppHelper.FormatMobile(CustomerMobile);
        var msg = Uri.EscapeDataString(
            $"Dear Customer,\n\nYour bill {LastBillNo} of ₹{NetAmount:N2} has been generated.\n\nThank you for visiting Shree Seva Medical!");
        var url = $"https://api.whatsapp.com/send?phone={formatted}&text={msg}";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        StatusMessage = "WhatsApp message sent.";
    }

    // ── Clear / Reset ────────────────────────────────────────────────────────
    [RelayCommand]
    private void Clear()
    {
        ClearCurrentSlot();
    }

    private void ClearCurrentSlot()
    {
        Items.Clear();
        CustomerName = string.Empty;
        CustomerMobile = string.Empty;
        PaymentMode = "CASH";
        AdjustmentAmount = 0;
        VoucherDate = DateTime.Today;
        ProductSearchText = string.Empty;
        ShowSearchDropdown = false;
        StatusMessage = string.Empty;

        // Update slot state
        _slotData[ActiveBillSlot].Items.Clear();
        _slotData[ActiveBillSlot].NetAmount = 0;
        UpdateSlotBadges();
    }

    // ── Load Pending ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void LoadPending()
    {
        // Cycle to next non-empty slot
        for (int i = 1; i <= 5; i++)
        {
            if (i != ActiveBillSlot && _slotData[i].Items.Any())
            {
                SwitchBillSlot(i);
                StatusMessage = $"Loaded pending bill from slot {i}.";
                return;
            }
        }
        StatusMessage = "No pending bills in other slots.";
    }
}
