namespace Medital_Application.Requests;

public class DateRangeRequest
{
    public DateTime FromDate { get; set; } = DateTime.Today;
    public DateTime ToDate { get; set; } = DateTime.Today;
    public int? AccountId { get; set; }
    public int? ProductId { get; set; }
    public string? FinancialYear { get; set; }
}
