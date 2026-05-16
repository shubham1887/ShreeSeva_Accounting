namespace Medital_Application.Models;

public class PurchaseDetail
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public int PurchaseMasterId { get; set; }
    public int ProductId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public string ExpiryMY { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal FreeQuantity { get; set; }
    public decimal SchemeQty { get; set; }
    public decimal ActualRate { get; set; }
    public decimal NetRate { get; set; }
    public decimal MRP { get; set; }
    public decimal SaleRate { get; set; }
    public decimal ItemDiscPer { get; set; }
    public decimal ItemDiscAmt { get; set; }
    public decimal SGSTRate { get; set; }
    public decimal CGSTRate { get; set; }
    public decimal IGSTRate { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string StockKey { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? ProductName { get; set; }
    public string? HSNCode { get; set; }
}
