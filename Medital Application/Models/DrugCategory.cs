namespace Medital_Application.Models;

public class DrugCategory
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public bool IsScheduled { get; set; }
    public bool IsHighRisk { get; set; }
    public bool IsTBMedicine { get; set; }
    public bool IsStockHold { get; set; }
    public decimal DefaultDiscount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
