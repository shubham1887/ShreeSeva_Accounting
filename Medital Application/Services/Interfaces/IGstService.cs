using Medital_Application.Requests;
using Medital_Application.Responses;

namespace Medital_Application.Services.Interfaces;

public interface IGstService
{
    (decimal SGST, decimal CGST, decimal IGST) CalculateGST(decimal taxableAmount, decimal gstRate, bool isInterState);
    Task<GSTReportResponse> GenerateGSTR1Async(DateRangeRequest request);
    Task<GSTReportResponse> GenerateGSTR3BAsync(DateRangeRequest request);
}
