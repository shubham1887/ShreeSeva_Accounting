namespace Medital_Application.Models;

public class SaleMaster
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public string TransactionType { get; set; } = "SA"; // SA=Sale, CR=CreditNote
    public int AccountId { get; set; }
    public int? PatientId { get; set; }
    public int? DoctorId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal ItemDiscAmount { get; set; }
    public decimal CashDiscPer { get; set; }
    public decimal CashDiscAmount { get; set; }
    public decimal TotalSGST { get; set; }
    public decimal TotalCGST { get; set; }
    public decimal TotalIGST { get; set; }
    public decimal RoundOff { get; set; }
    public decimal NetAmount { get; set; }
    public string PaymentMode { get; set; } = "CASH"; // CASH/CREDIT/CHEQUE/UPI
    public string? ChequeNo { get; set; }
    public DateTime? ChequeDate { get; set; }
    public string? UPIRef { get; set; }
    public string? Narration { get; set; }
    public string FinancialYear { get; set; } = "2425";
    public bool IsInterState { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? AccountName { get; set; }
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
    public List<SaleDetail> Details { get; set; } = new();
}
