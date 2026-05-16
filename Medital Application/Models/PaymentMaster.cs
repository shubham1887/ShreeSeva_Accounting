namespace Medital_Application.Models;

public class PaymentMaster
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMode { get; set; } = "CASH";
    public string? ChequeNo { get; set; }
    public DateTime? ChequeDate { get; set; }
    public int? OurBankId { get; set; }
    public string? Narration { get; set; }
    public string FinancialYear { get; set; } = "2425";
    public bool IsCancelled { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? AccountName { get; set; }
    public List<PaymentDetail> Details { get; set; } = new();
}
