namespace Medital_Application.Requests;

public class CreatePurchaseRequest
{
    public int AccountId { get; set; }
    public string? BillNo { get; set; }
    public DateTime? BillDate { get; set; }
    public string? ChallanNo { get; set; }
    public DateTime? ChallanDate { get; set; }
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public List<PurchaseItemRequest> PurchaseItems { get; set; } = new();
    public decimal SpecialDisc { get; set; }
    public decimal FreightAmount { get; set; }
    public string? Narration { get; set; }
    public bool IsInterState { get; set; }
    public string FinancialYear { get; set; } = "2425";
    public int CreatedByUserId { get; set; }
}
