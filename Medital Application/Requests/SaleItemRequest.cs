namespace Medital_Application.Requests;

public class SaleItemRequest
{
    public int ProductId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public string ExpiryMY { get; set; } = string.Empty;
    public int? StockId { get; set; }
    public decimal Quantity { get; set; }
    public decimal FreeQty { get; set; }
    public decimal SaleRate { get; set; }
    public decimal MRP { get; set; }
    public decimal ItemDiscPer { get; set; }
    public decimal ItemDiscAmt { get; set; }
    public decimal SGSTRate { get; set; }
    public decimal CGSTRate { get; set; }
    public decimal IGSTRate { get; set; }
    public decimal PurchaseRate { get; set; }
    public string? HSNCode { get; set; }
    public string? ProductName { get; set; }
}
