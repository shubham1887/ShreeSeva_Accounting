namespace Medital_Application.Responses;

public class StockResponse
{
    public int StockId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ManufacturerName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public string ExpiryMY { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public decimal CurrentQty { get; set; }
    public decimal ActualRate { get; set; }
    public decimal SaleRate { get; set; }
    public decimal MRP { get; set; }
    public string ExpiryStatus { get; set; } = "GOOD"; // EXPIRED/EXPIRING_SOON/GOOD
    public bool IsLowStock { get; set; }
    public decimal MinQty { get; set; }
    public string? HSNCode { get; set; }
    public decimal StockValue => CurrentQty * ActualRate;
}
