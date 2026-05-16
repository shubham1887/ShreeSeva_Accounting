namespace Medital_Application.Models;

public class QuotationDetail
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public int QuotationMasterId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal SaleRate { get; set; }
    public decimal DiscPer { get; set; }
    public decimal SGSTRate { get; set; }
    public decimal CGSTRate { get; set; }
    public decimal IGSTRate { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? ProductName { get; set; }
}
