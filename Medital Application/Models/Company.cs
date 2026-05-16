namespace Medital_Application.Models;

public class Company
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string CompanyName { get; set; } = string.Empty;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? StateCode { get; set; }
    public string? PinCode { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? GSTIN { get; set; }
    public string? DrugLicense { get; set; }
    public string? PAN { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNo { get; set; }
    public string? IFSCCode { get; set; }
    public string? UPIId { get; set; }
    public string FinancialYear { get; set; } = "2425";
    public string? YearStart { get; set; }
    public string? YearEnd { get; set; }
    public string? LogoPath { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
