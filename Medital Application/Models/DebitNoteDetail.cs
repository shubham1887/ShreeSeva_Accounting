namespace Medital_Application.Models;

public class DebitNoteDetail
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public int DebitNoteMasterId { get; set; }
    public int ProductId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public string ExpiryMY { get; set; } = string.Empty;
    public decimal ReturnQty { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal MRP { get; set; }
    public decimal SGSTRate { get; set; }
    public decimal CGSTRate { get; set; }
    public decimal IGSTRate { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? ProductName { get; set; }
}
