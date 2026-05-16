namespace Medital_Application.Models;

public class ReceiptDetail
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public int ReceiptMasterId { get; set; }
    public int? SaleMasterId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? VoucherNo { get; set; }
    public DateTime? VoucherDate { get; set; }
    public decimal? VoucherAmount { get; set; }
}
