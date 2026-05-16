namespace Medital_Application.Requests;

public class CreateReceiptRequest
{
    public int AccountId { get; set; }
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string PaymentMode { get; set; } = "CASH";
    public string? ChequeNo { get; set; }
    public DateTime? ChequeDate { get; set; }
    public int? OurBankId { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string? Narration { get; set; }
    public string FinancialYear { get; set; } = "2425";
    public List<ReceiptAllocation> Allocations { get; set; } = new();
}

public class ReceiptAllocation
{
    public int SaleMasterId { get; set; }
    public decimal AllocatedAmount { get; set; }
}
