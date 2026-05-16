using Medital_Application.Helpers;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Requests;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;

namespace Medital_Application.Services;

public class GstService : IGstService
{
    private readonly IReportRepository _reports;

    public GstService(IReportRepository reports) => _reports = reports;

    public (decimal SGST, decimal CGST, decimal IGST) CalculateGST(decimal taxableAmount, decimal gstRate, bool isInterState)
    {
        if (isInterState)
            return (0, 0, Math.Round(taxableAmount * gstRate / 100, 2));
        var half = Math.Round(taxableAmount * gstRate / 200, 2);
        return (half, half, 0);
    }

    public async Task<GSTReportResponse> GenerateGSTR1Async(DateRangeRequest request)
    {
        // GSTR-1: Outward supplies (sales)
        var report = await _reports.GetGSTReportAsync(request);
        report.Period = $"GSTR-1: {report.Period}";
        return report;
    }

    public async Task<GSTReportResponse> GenerateGSTR3BAsync(DateRangeRequest request)
    {
        // GSTR-3B: Summary return with net tax liability
        var report = await _reports.GetGSTReportAsync(request);
        report.NetTaxPayable = report.TotalGST - report.PurchaseSGST - report.PurchaseCGST - report.PurchaseIGST;
        report.Period = $"GSTR-3B: {report.Period}";
        return report;
    }
}
