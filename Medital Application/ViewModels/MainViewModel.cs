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
    [ObservableProperty] private string _financialYear = "2425";
    [ObservableProperty] private string _currentDate = DateTime.Today.ToString("dd-MMM-yyyy");
    [ObservableProperty] private string _currentModuleName = "Dashboard";
    [ObservableProperty] private UserControl? _currentView;
    [ObservableProperty] private bool _isSidebarExpanded = true;
    [ObservableProperty] private string _statusMessage = "Ready";

    public int CurrentUserId { get; private set; }
    public UserRight? CurrentUserRights { get; private set; }

    public MainViewModel(INavigationService navigation, IBackupService backup, IServiceProvider services)
    {
        _navigation = navigation;
        _backup = backup;
        _services = services;

        _navigation.NavigationChanged += OnNavigationChanged;
    }

    public void SetCurrentUser(LoginResponse login)
    {
        UserName = login.UserName;
        FinancialYear = login.FinancialYear;
        CompanyName = login.CompanyName;
        CurrentUserId = login.UserId;
        CurrentUserRights = login.Rights;
    }

    private void OnNavigationChanged(object? sender, UserControl view)
    {
        CurrentView = view;
    }

    [RelayCommand]
    private void NavigateDashboard()
    {
        CurrentModuleName = "Dashboard";
        _navigation.NavigateTo<Views.DashboardView>();
    }

    [RelayCommand]
    private void NavigateSale()
    {
        CurrentModuleName = "New Sale";
        _navigation.NavigateTo<Views.Sales.SaleEntryView>();
    }

    [RelayCommand]
    private void NavigateSaleList()
    {
        CurrentModuleName = "Sale List";
        _navigation.NavigateTo<Views.Sales.SaleListView>();
    }

    [RelayCommand]
    private void NavigatePurchase()
    {
        CurrentModuleName = "New Purchase";
        _navigation.NavigateTo<Views.Purchase.PurchaseEntryView>();
    }

    [RelayCommand]
    private void NavigatePurchaseList()
    {
        CurrentModuleName = "Purchase List";
        _navigation.NavigateTo<Views.Purchase.PurchaseListView>();
    }

    [RelayCommand]
    private void NavigateStock()
    {
        CurrentModuleName = "Stock";
        _navigation.NavigateTo<Views.Stock.StockView>();
    }

    [RelayCommand]
    private void NavigateAccounts()
    {
        CurrentModuleName = "Accounts";
        _navigation.NavigateTo<Views.Accounts.AccountListView>();
    }

    [RelayCommand]
    private void NavigateReceipt()
    {
        CurrentModuleName = "Receipt";
        _navigation.NavigateTo<Views.Accounts.ReceiptView>();
    }

    [RelayCommand]
    private void NavigatePayment()
    {
        CurrentModuleName = "Payment";
        _navigation.NavigateTo<Views.Accounts.PaymentView>();
    }

    [RelayCommand]
    private void NavigateReports()
    {
        CurrentModuleName = "Reports";
        _navigation.NavigateTo<Views.Reports.SalesReportView>();
    }

    [RelayCommand]
    private void NavigateGST()
    {
        CurrentModuleName = "GST Report";
        _navigation.NavigateTo<Views.Reports.GSTReportView>();
    }

    [RelayCommand]
    private void NavigateSettings()
    {
        CurrentModuleName = "Settings";
        _navigation.NavigateTo<Views.Settings.SettingsView>();
    }

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        StatusMessage = "Creating backup...";
        var ok = await _backup.CreateBackupAsync();
        StatusMessage = ok ? "Backup created successfully." : "Backup failed.";
    }

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;
}
