namespace Medital_Application.Models;

public class Doctor
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string DoctorCode { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? RegNo { get; set; }
    public decimal IncentivePer { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
