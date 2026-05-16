using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;

    [ObservableProperty] private decimal _todaySales;
    [ObservableProperty] private decimal _todayPurchase;
    [ObservableProperty] private decimal _monthSales;
    [ObservableProperty] private decimal _pendingRecovery;
    [ObservableProperty] private decimal _pendingPayment;
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private int _expiringCount;
    [ObservableProperty] private int _expiredCount;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _lastRefreshed = string.Empty;

    public ObservableCollection<AlertItem> LowStockItems { get; } = new();
    public ObservableCollection<AlertItem> ExpiringItems { get; } = new();
    public ObservableCollection<RecentTransaction> RecentTransactions { get; } = new();

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var data = await _dashboardService.GetDashboardAsync();
            TodaySales = data.TodaySales;
            TodayPurchase = data.TodayPurchase;
            MonthSales = data.MonthSales;
            PendingRecovery = data.PendingRecovery;
            PendingPayment = data.PendingPayment;
            LowStockCount = data.LowStockCount;
            ExpiringCount = data.ExpiringCount;
            ExpiredCount = data.ExpiredCount;
            LastRefreshed = DateTime.Now.ToString("hh:mm tt");

            LowStockItems.Clear();
            foreach (var item in data.LowStockItems) LowStockItems.Add(item);

            ExpiringItems.Clear();
            foreach (var item in data.ExpiringItems) ExpiringItems.Add(item);

            RecentTransactions.Clear();
            foreach (var tx in data.RecentTransactions) RecentTransactions.Add(tx);
        }
        catch (Exception ex)
        {
            // Log
        }
        finally
        {
            IsLoading = false;
        }
    }
}
