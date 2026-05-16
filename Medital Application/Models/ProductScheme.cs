namespace Medital_Application.Models;

public class ProductScheme
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public int ProductId { get; set; }
    public decimal BuyQty1 { get; set; }
    public decimal FreeQty1 { get; set; }
    public decimal DiscPer1 { get; set; }
    public decimal BuyQty2 { get; set; }
    public decimal FreeQty2 { get; set; }
    public decimal DiscPer2 { get; set; }
    public decimal BuyQty3 { get; set; }
    public decimal FreeQty3 { get; set; }
    public decimal DiscPer3 { get; set; }
    public string? ValidFrom { get; set; }
    public string? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? ProductName { get; set; }
}
