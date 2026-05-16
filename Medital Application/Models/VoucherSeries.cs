namespace Medital_Application.Models;

public class VoucherSeries
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string VoucherType { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = "2425";
    public int CurrentNo { get; set; }
    public int Padding { get; set; } = 5;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string GetNextVoucherNo()
    {
        int next = CurrentNo + 1;
        return $"{Prefix}/{FinancialYear}/{next.ToString().PadLeft(Padding, '0')}";
    }
}
