namespace Medital_Application.Requests;

public class CreateCreditNoteRequest
{
    public int AccountId { get; set; }
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public string? RefVoucherNo { get; set; }
    public List<CreditNoteItemRequest> Items { get; set; } = new();
    public string? Narration { get; set; }
    public string FinancialYear { get; set; } = "2425";
}

public class CreditNoteItemRequest
{
    public int ProductId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public string ExpiryMY { get; set; } = string.Empty;
    public decimal ReturnQty { get; set; }
    public decimal SaleRate { get; set; }
    public decimal MRP { get; set; }
    public decimal SGSTRate { get; set; }
    public decimal CGSTRate { get; set; }
    public decimal IGSTRate { get; set; }
}
