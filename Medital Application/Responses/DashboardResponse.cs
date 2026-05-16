namespace Medital_Application.Responses;

public class AlertItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public string ExpiryMY { get; set; } = string.Empty;
    public decimal CurrentQty { get; set; }
    public decimal MinQty { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RecentTransaction
{
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class DashboardResponse
{
    public decimal TodaySales { get; set; }
    public decimal TodayPurchase { get; set; }
    public decimal MonthSales { get; set; }
    public decimal MonthPurchase { get; set; }
    public decimal PendingRecovery { get; set; }    // Total outstanding from customers
    public decimal PendingPayment { get; set; }     // Total outstanding to suppliers
    public int LowStockCount { get; set; }
    public int ExpiringCount { get; set; }
    public int ExpiredCount { get; set; }
    public List<AlertItem> LowStockItems { get; set; } = new();
    public List<AlertItem> ExpiringItems { get; set; } = new();
    public List<RecentTransaction> RecentTransactions { get; set; } = new();
    public DateTime AsOf { get; set; } = DateTime.Now;
}
