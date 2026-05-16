namespace Medital_Application.Models;

public class PurchaseMaster
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public string? BillNo { get; set; }
    public DateTime? BillDate { get; set; }
    public string? ChallanNo { get; set; }
    public DateTime? ChallanDate { get; set; }
    public int AccountId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal ItemDiscAmount { get; set; }
    public decimal SpecialDisc { get; set; }
    public decimal FreightAmount { get; set; }
    public decimal TotalSGST { get; set; }
    public decimal TotalCGST { get; set; }
    public decimal TotalIGST { get; set; }
    public decimal RoundOff { get; set; }
    public decimal NetAmount { get; set; }
    public string FinancialYear { get; set; } = "2425";
    public string? Narration { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? AccountName { get; set; }
    public List<PurchaseDetail> Details { get; set; } = new();
}
