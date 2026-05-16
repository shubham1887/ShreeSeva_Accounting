namespace Medital_Application.Responses;

public class LedgerTransaction
{
    public DateTime Date { get; set; }
    public string VoucherNo { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Narration { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public bool IsDebitBalance { get; set; }
}

public class AccountLedgerResponse
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool OpeningIsDebit { get; set; }
    public List<LedgerTransaction> Transactions { get; set; } = new();
    public decimal ClosingBalance { get; set; }
    public bool ClosingIsDebit { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
}
