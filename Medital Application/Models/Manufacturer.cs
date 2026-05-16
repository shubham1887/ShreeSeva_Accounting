namespace Medital_Application.Models;

public class Manufacturer
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email1 { get; set; }
    public string? Email2 { get; set; }
    public string? Email3 { get; set; }
    public string? Phone { get; set; }
    public bool IsManufacturer { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
