namespace Medital_Application.Models;

public class Product
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? MarathiName { get; set; }
    public string? Barcode { get; set; }
    public string Unit { get; set; } = "NOS";
    public int PackSize { get; set; } = 1;
    public int? ManufacturerId { get; set; }
    public int? DrugCategoryId { get; set; }
    public string? HSNCode { get; set; }
    public decimal SGSTRate { get; set; }
    public decimal CGSTRate { get; set; }
    public decimal IGSTRate { get; set; }
    public bool IsFixedRate { get; set; }
    public decimal Margin { get; set; }
    public decimal MinQty { get; set; }
    public decimal MaxQty { get; set; }
    public bool IsNonRx { get; set; } = true;
    public bool IsScheduled { get; set; }
    public bool IsHighRisk { get; set; }
    public decimal DefaultSaleRate { get; set; }
    public decimal DefaultMRP { get; set; }
    public decimal LastPurchaseRate { get; set; }
    public decimal CurrentQty { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation (not persisted, populated by JOIN)
    public string? ManufacturerName { get; set; }
    public string? CategoryName { get; set; }
}
