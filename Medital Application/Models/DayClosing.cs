namespace Medital_Application.Models;

public class DayClosing
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string ClosingDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public string? ShiftStart { get; set; }
    public string? ShiftClose { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal CashSales { get; set; }
    public decimal ClosingCash { get; set; }
    public string? Notes { get; set; }
    public int Denom2000 { get; set; }
    public int Denom500 { get; set; }
    public int Denom200 { get; set; }
    public int Denom100 { get; set; }
    public int Denom50 { get; set; }
    public int Denom20 { get; set; }
    public int Denom10 { get; set; }
    public int Denom5 { get; set; }
    public int Denom2 { get; set; }
    public int Denom1 { get; set; }
    public int? OperatorId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public decimal TotalDenomination =>
        Denom2000 * 2000m + Denom500 * 500m + Denom200 * 200m +
        Denom100 * 100m + Denom50 * 50m + Denom20 * 20m +
        Denom10 * 10m + Denom5 * 5m + Denom2 * 2m + Denom1 * 1m;
}
