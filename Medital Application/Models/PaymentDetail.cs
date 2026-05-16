namespace Medital_Application.Models;

public class PaymentDetail
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public int PaymentMasterId { get; set; }
    public int? PurchaseMasterId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? VoucherNo { get; set; }
    public DateTime? VoucherDate { get; set; }
    public decimal? VoucherAmount { get; set; }
}
