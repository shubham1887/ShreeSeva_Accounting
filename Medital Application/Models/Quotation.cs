namespace Medital_Application.Models;

public class Quotation
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public int AccountId { get; set; }
    public int ValidDays { get; set; } = 7;
    public decimal NetAmount { get; set; }
    public bool IsConverted { get; set; }
    public string? ConvertedVchNo { get; set; }
    public string? Narration { get; set; }
    public string FinancialYear { get; set; } = "2425";
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? AccountName { get; set; }
    public List<QuotationDetail> Details { get; set; } = new();
}
