namespace Medital_Application.Responses;

public class ProfitLossResponse
{
    public string Period { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal GrossSales { get; set; }
    public decimal SalesReturns { get; set; }
    public decimal NetSales { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossProfitPer { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal NetProfitPer { get; set; }
    public decimal OpeningStock { get; set; }
    public decimal Purchases { get; set; }
    public decimal PurchaseReturns { get; set; }
    public decimal ClosingStock { get; set; }
}
