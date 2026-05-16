using Medital_Application.Requests;
using Medital_Application.Responses;

namespace Medital_Application.Services.Interfaces;

public interface IReportService
{
    Task<List<SaleResponse>> GetSalesReportAsync(DateRangeRequest request);
    Task<List<PurchaseResponse>> GetPurchaseReportAsync(DateRangeRequest request);
    Task<List<StockResponse>> GetStockReportAsync(string? searchTerm = null);
    Task<ProfitLossResponse> GetProfitLossAsync(DateRangeRequest request);
    Task<AccountLedgerResponse> GetLedgerAsync(int accountId, DateRangeRequest request);
    Task<List<RecentTransaction>> GetDayBookAsync(DateTime date);
    Task<DashboardResponse> GetDashboardAsync();
}
