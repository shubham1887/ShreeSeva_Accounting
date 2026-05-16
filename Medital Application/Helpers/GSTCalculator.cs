namespace Medital_Application.Helpers;

public static class GSTCalculator
{
    public static readonly decimal[] ValidSlabs = { 0m, 5m, 12m, 18m, 28m };

    public record GSTResult(decimal SGSTAmount, decimal CGSTAmount, decimal IGSTAmount, decimal TotalGST);

    public static GSTResult Calculate(decimal taxableAmount, decimal sgstRate, decimal cgstRate, decimal igstRate, bool isInterState)
    {
        if (taxableAmount <= 0) return new GSTResult(0, 0, 0, 0);

        if (isInterState)
        {
            var igst = Math.Round(taxableAmount * igstRate / 100, 2);
            return new GSTResult(0, 0, igst, igst);
        }
        else
        {
            var sgst = Math.Round(taxableAmount * sgstRate / 100, 2);
            var cgst = Math.Round(taxableAmount * cgstRate / 100, 2);
            return new GSTResult(sgst, cgst, 0, sgst + cgst);
        }
    }

    /// <summary>
    /// Given a total GST rate like 18%, split into SGST=9, CGST=9 (intrastate) or IGST=18 (interstate).
    /// </summary>
    public static (decimal SGST, decimal CGST, decimal IGST) SplitGSTRate(decimal totalGSTRate, bool isInterState)
    {
        if (isInterState) return (0, 0, totalGSTRate);
        var half = totalGSTRate / 2;
        return (half, half, 0);
    }

    public static decimal GetGSTRate(decimal sgst, decimal cgst, decimal igst) =>
        igst > 0 ? igst : (sgst + cgst);

    public static bool IsValidSlab(decimal rate) => ValidSlabs.Contains(rate);
}
