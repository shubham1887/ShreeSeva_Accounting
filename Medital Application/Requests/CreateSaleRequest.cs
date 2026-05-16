namespace Medital_Application.Requests;

public class CreateSaleRequest
{
    public int AccountId { get; set; }
    public int? PatientId { get; set; }
    public int? DoctorId { get; set; }
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public List<SaleItemRequest> SaleItems { get; set; } = new();
    public decimal CashDiscountPer { get; set; }
    public string PaymentMode { get; set; } = "CASH"; // CASH/CREDIT/CHEQUE/UPI
    public string? ChequeNo { get; set; }
    public DateTime? ChequeDate { get; set; }
    public string? UPIRef { get; set; }
    public string? Narration { get; set; }
    public bool IsInterState { get; set; }
    public string FinancialYear { get; set; } = "2425";
    public int CreatedByUserId { get; set; }
}
