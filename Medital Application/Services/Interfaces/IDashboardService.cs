using Medital_Application.Responses;

namespace Medital_Application.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync();
    Task<decimal> GetTodaySalesAsync();
    Task<decimal> GetTodayPurchaseAsync();
    Task<decimal> GetPendingRecoveryAsync();
    Task<decimal> GetPendingPaymentAsync();
    Task<int> GetLowStockCountAsync();
    Task<int> GetExpiringCountAsync();
}
