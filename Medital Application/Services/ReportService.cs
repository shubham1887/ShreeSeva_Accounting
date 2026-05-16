using Medital_Application.Repositories.Interfaces;
using Medital_Application.Requests;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;

namespace Medital_Application.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reports;

    public ReportService(IReportRepository reports) => _reports = reports;

    public Task<List<SaleResponse>> GetSalesReportAsync(DateRangeRequest request)
        => _reports.GetSalesReportAsync(request);

    public Task<List<PurchaseResponse>> GetPurchaseReportAsync(DateRangeRequest request)
        => _reports.GetPurchaseReportAsync(request);

    public Task<List<StockResponse>> GetStockReportAsync(string? searchTerm = null)
        => _reports.GetStockReportAsync(searchTerm);

    public Task<ProfitLossResponse> GetProfitLossAsync(DateRangeRequest request)
        => _reports.GetProfitLossAsync(request);

    public Task<AccountLedgerResponse> GetLedgerAsync(int accountId, DateRangeRequest request)
        => _reports.GetLedgerAsync(accountId, request);

    public Task<List<RecentTransaction>> GetDayBookAsync(DateTime date)
        => _reports.GetDayBookAsync(date);

    public Task<DashboardResponse> GetDashboardAsync()
        => _reports.GetDashboardDataAsync();
}
