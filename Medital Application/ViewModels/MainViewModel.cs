using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Responses;
using Medital_Application.Services;
using Medital_Application.Services.Interfaces;
using System.Windows.Controls;

namespace Medital_Application.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IBackupService _backup;
    private readonly IServiceProvider _services;

    [ObservableProperty] private string _companyName = "Shree Seva Medical";
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private string _userInitial = "A";
    [ObservableProperty] private string _financialYear = "2526";
    [ObservableProperty] private string _currentDate = DateTime.Today.ToString("dd-MMM-yyyy");
    [ObservableProperty] private string _currentModuleName = "Dashboard";
    [ObservableProperty] private UserControl? _currentView;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private string _alertTicker = "Loading alerts...";
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private int _expiringCount;

    public int CurrentUserId { get; private set; }
    public UserRight? CurrentUserRights { get; private set; }

    public MainViewModel(INavigationService navigation, IBackupService backup, IServiceProvider services)
    {
        _navigation = navigation;
        _backup = backup;
        _services = services;
        _navigation.NavigationChanged += (_, view) => CurrentView = view;
    }

    public void SetCurrentUser(LoginResponse login)
    {
        UserName = login.UserName;
        FinancialYear = login.FinancialYear;
        CompanyName = login.CompanyName;
        CurrentUserId = login.UserId;
        CurrentUserRights = login.Rights;
        UserInitial = string.IsNullOrEmpty(login.UserName) ? "A"
            : login.UserName[0].ToString().ToUpper();
    }

    // ── Billing ──────────────────────────────────────────────────────────
    [RelayCommand]
    private void NavigateCounterSale()
    {
        CurrentModuleName = "Counter Sale";
        TryNavigate<Views.Sales.CounterSaleView>();
    }

    [RelayCommand]
    private void NavigateSale()
    {
        CurrentModuleName = "Party Sale";
        TryNavigate<Views.Sales.SaleEntryView>();
    }

    [RelayCommand]
    private void NavigateSaleList()
    {
        CurrentModuleName = "Sale List";
        TryNavigate<Views.Sales.SaleListView>();
    }

    [RelayCommand]
    private void NavigateSaleReturn()
    {
        CurrentModuleName = "Sales Return";
        StatusMessage = "Sales Return module coming soon.";
    }

    // ── Purchase ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void NavigatePurchase()
    {
        CurrentModuleName = "New Purchase";
        TryNavigate<Views.Purchase.PurchaseEntryView>();
    }

    [RelayCommand]
    private void NavigatePurchaseList()
    {
        CurrentModuleName = "Purchase List";
        TryNavigate<Views.Purchase.PurchaseListView>();
    }

    [RelayCommand]
    private void NavigatePurchaseReturn()
    {
        CurrentModuleName = "Purchase Return";
        StatusMessage = "Purchase Return module coming soon.";
    }

    // ── Inventory ────────────────────────────────────────────────────────
    [RelayCommand]
    private void NavigateStock()
    {
        CurrentModuleName = "Stock";
        TryNavigate<Views.Stock.StockView>();
    }

    [RelayCommand]
    private void NavigateNearExpiry()
    {
        CurrentModuleName = "Near Expiry";
        StatusMessage = "Near Expiry module coming soon.";
    }

    [RelayCommand]
    private void NavigateBarcode()
    {
        CurrentModuleName = "Barcode Print";
        TryNavigate<Views.Masters.BarcodePrintView>();
    }

    // ── Accounts ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void NavigateAccounts()
    {
        CurrentModuleName = "Accounts";
        TryNavigate<Views.Accounts.AccountListView>();
    }

    [RelayCommand]
    private void NavigateReceipt()
    {
        CurrentModuleName = "Receipt";
        TryNavigate<Views.Accounts.ReceiptView>();
    }

    [RelayCommand]
    private void NavigatePayment()
    {
        CurrentModuleName = "Payment";
        TryNavigate<Views.Accounts.PaymentView>();
    }

    [RelayCommand]
    private void NavigateJournal()
    {
        CurrentModuleName = "Journal";
        StatusMessage = "Journal module coming soon.";
    }

    // ── Masters ──────────────────────────────────────────────────────────
    [RelayCommand]
    private void NavigateProducts()
    {
        CurrentModuleName = "Product Master";
        TryNavigate<Views.Masters.ProductMasterView>();
    }

    [RelayCommand]
    private void NavigateManufacturers()
    {
        CurrentModuleName = "Manufacturers";
        StatusMessage = "Manufacturers module coming soon.";
    }

    [RelayCommand]
    private void NavigateDoctors()
    {
        CurrentModuleName = "Doctors";
        StatusMessage = "Doctors module coming soon.";
    }

    [RelayCommand]
    private void NavigatePatients()
    {
        CurrentModuleName = "Patients";
        StatusMessage = "Patients module coming soon.";
    }

    // ── Reports ──────────────────────────────────────────────────────────
    [RelayCommand]
    private void NavigateDashboard()
    {
        CurrentModuleName = "Dashboard";
        TryNavigate<Views.DashboardView>();
    }

    [RelayCommand]
    private void NavigateReports()
    {
        CurrentModuleName = "Sales Report";
        TryNavigate<Views.Reports.SalesReportView>();
    }

    [RelayCommand]
    private void NavigateGST()
    {
        CurrentModuleName = "GST Report";
        TryNavigate<Views.Reports.GSTReportView>();
    }

    // ── System ───────────────────────────────────────────────────────────
    [RelayCommand]
    private void NavigateSettings()
    {
        CurrentModuleName = "Settings";
        TryNavigate<Views.Settings.SettingsView>();
    }

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        StatusMessage = "Creating backup...";
        var ok = await _backup.CreateBackupAsync();
        StatusMessage = ok ? "Backup created successfully." : "Backup failed.";
    }

    // ── Helper ───────────────────────────────────────────────────────────
    private void TryNavigate<TView>() where TView : UserControl
    {
        try
        {
            _navigation.NavigateTo<TView>();
        }
        catch (InvalidOperationException)
        {
            StatusMessage = $"{typeof(TView).Name} is not registered. Coming soon.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Navigation error: {ex.Message}";
        }
    }
}
