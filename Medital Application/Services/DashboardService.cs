using Medital_Application.Repositories.Interfaces;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;

namespace Medital_Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ISaleRepository _saleRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IStockRepository _stockRepo;
    private readonly IAccountRepository _accountRepo;

    public DashboardService(
        ISaleRepository saleRepo,
        IPurchaseRepository purchaseRepo,
        IStockRepository stockRepo,
        IAccountRepository accountRepo)
    {
        _saleRepo = saleRepo;
        _purchaseRepo = purchaseRepo;
        _stockRepo = stockRepo;
        _accountRepo = accountRepo;
    }

    public async Task<DashboardResponse> GetDashboardAsync()
    {
        var today = DateTime.Today;
        var todaySales = await _saleRepo.GetTotalByDateAsync(today);
        var todayPurchase = await _purchaseRepo.GetTotalByDateAsync(today);
        var monthSales = await _saleRepo.GetTotalByMonthAsync(today.Year, today.Month);

        var lowStocks = await _stockRepo.GetLowStockAsync();
        var expiringStocks = await _stockRepo.GetExpiringAsync(3);
        var expiredStocks = await _stockRepo.GetExpiredAsync();

        var recentSales = await _saleRepo.GetByDateRangeAsync(today.AddDays(-7), today);
        var recentTx = recentSales.Take(10).Select(s => new RecentTransaction
        {
            VoucherNo = s.VoucherNo,
            Date = s.VoucherDate,
            Type = "Sale",
            AccountName = s.AccountName ?? "",
            Amount = s.NetAmount,
        }).ToList();

        return new DashboardResponse
        {
            TodaySales = todaySales,
            TodayPurchase = todayPurchase,
            MonthSales = monthSales,
            PendingRecovery = 0, // computed separately if needed
            PendingPayment = 0,
            LowStockCount = lowStocks.Count,
            ExpiringCount = expiringStocks.Count,
            ExpiredCount = expiredStocks.Count,
            LowStockItems = lowStocks.Take(10).Select(s => new AlertItem
            {
                ProductId = s.ProductId,
                ProductName = s.ProductName ?? "",
                BatchNo = s.BatchNo,
                ExpiryMY = s.ExpiryMY,
                CurrentQty = s.CurrentQty,
                Status = "LOW",
            }).ToList(),
            ExpiringItems = expiringStocks.Take(10).Select(s => new AlertItem
            {
                ProductId = s.ProductId,
                ProductName = s.ProductName ?? "",
                BatchNo = s.BatchNo,
                ExpiryMY = s.ExpiryMY,
                CurrentQty = s.CurrentQty,
                Status = s.IsExpired ? "EXPIRED" : "EXPIRING_SOON",
            }).ToList(),
            RecentTransactions = recentTx,
            AsOf = DateTime.Now,
        };
    }

    public Task<decimal> GetTodaySalesAsync() => _saleRepo.GetTotalByDateAsync(DateTime.Today);
    public Task<decimal> GetTodayPurchaseAsync() => _purchaseRepo.GetTotalByDateAsync(DateTime.Today);
    public async Task<decimal> GetPendingRecoveryAsync() => 0; // simplified
    public async Task<decimal> GetPendingPaymentAsync() => 0; // simplified
    public async Task<int> GetLowStockCountAsync() => (await _stockRepo.GetLowStockAsync()).Count;
    public async Task<int> GetExpiringCountAsync() => (await _stockRepo.GetExpiringAsync(3)).Count;
}
