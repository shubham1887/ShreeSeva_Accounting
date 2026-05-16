namespace Medital_Application.Models;

public class Stock
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public int ProductId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public string ExpiryMY { get; set; } = string.Empty;  // MM/YYYY
    public string ExpiryDate { get; set; } = string.Empty; // ISO date
    public string GodownCode { get; set; } = "MAIN";
    public decimal ActualRate { get; set; }
    public decimal NetRate { get; set; }
    public decimal MRP { get; set; }
    public decimal SaleRate { get; set; }
    public decimal OpeningQty { get; set; }
    public decimal PurchasedQty { get; set; }
    public decimal SoldQty { get; set; }
    public decimal CreditNoteQty { get; set; }
    public decimal StockInQty { get; set; }
    public decimal StockOutQty { get; set; }
    public string StockKey { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Computed
    public decimal CurrentQty =>
        OpeningQty + PurchasedQty + CreditNoteQty + StockInQty - SoldQty - StockOutQty;

    public bool IsExpired => DateTime.TryParse(ExpiryDate, out var d) && d < DateTime.Today;
    public bool IsExpiringSoon(int months = 3) =>
        DateTime.TryParse(ExpiryDate, out var d) && d >= DateTime.Today && d <= DateTime.Today.AddMonths(months);

    // Navigation
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public string? ManufacturerName { get; set; }
    public string? CategoryName { get; set; }
    public string? HSNCode { get; set; }
}
