namespace Medital_Application.Requests;

public class CreatePaymentRequest
{
    public int AccountId { get; set; }
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string PaymentMode { get; set; } = "CASH";
    public string? ChequeNo { get; set; }
    public DateTime? ChequeDate { get; set; }
    public int? OurBankId { get; set; }
    public string? Narration { get; set; }
    public string FinancialYear { get; set; } = "2425";
    public List<PaymentAllocation> Allocations { get; set; } = new();
}

public class PaymentAllocation
{
    public int PurchaseMasterId { get; set; }
    public decimal AllocatedAmount { get; set; }
}
