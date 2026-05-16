using Medital_Application.Responses;
using Medital_Application.Requests;

namespace Medital_Application.Repositories.Interfaces;

public interface IReportRepository
{
    Task<List<SaleResponse>> GetSalesReportAsync(DateRangeRequest request);
    Task<List<PurchaseResponse>> GetPurchaseReportAsync(DateRangeRequest request);
    Task<List<StockResponse>> GetStockReportAsync(string? searchTerm = null);
    Task<GSTReportResponse> GetGSTReportAsync(DateRangeRequest request);
    Task<ProfitLossResponse> GetProfitLossAsync(DateRangeRequest request);
    Task<AccountLedgerResponse> GetLedgerAsync(int accountId, DateRangeRequest request);
    Task<List<RecentTransaction>> GetDayBookAsync(DateTime date);
    Task<DashboardResponse> GetDashboardDataAsync();
}
