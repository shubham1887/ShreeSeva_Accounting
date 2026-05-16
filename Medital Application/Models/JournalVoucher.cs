namespace Medital_Application.Models;

public class JournalVoucher
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public int AccountId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Narration { get; set; }
    public string FinancialYear { get; set; } = "2425";
    public bool IsCancelled { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? AccountName { get; set; }
}
