namespace Medital_Application.Models;

public class Account
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? Address4 { get; set; }
    public string? Area { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? StateCode { get; set; }
    public string? PinCode { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? GSTIN { get; set; }
    public int? GroupId { get; set; }
    public decimal CashDiscountPer { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNo { get; set; }
    public string? IFSCCode { get; set; }
    public string? DrugLicenseNo { get; set; }
    public string? PANNo { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool OpeningDr { get; set; }
    public int DueDays { get; set; }
    public bool IsLocked { get; set; }
    public bool IsInactive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? GroupName { get; set; }
    public string? GroupCode { get; set; }
}
