namespace Medital_Application.Models;

public class Patient
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string PatientCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public int? DoctorId { get; set; }
    public string? BloodGroup { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? DoctorName { get; set; }

    public int? Age =>
        string.IsNullOrEmpty(DateOfBirth) ? null
        : DateTime.TryParse(DateOfBirth, out var dob)
            ? (int)((DateTime.Today - dob).TotalDays / 365.25)
            : null;
}
