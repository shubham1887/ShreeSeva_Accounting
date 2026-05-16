namespace Medital_Application.Models;

public class DiscountStructure
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public decimal ToAmount { get; set; }
    public decimal DiscountPer { get; set; }
    public decimal ProfitLow { get; set; }
    public decimal ProfitHigh { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
