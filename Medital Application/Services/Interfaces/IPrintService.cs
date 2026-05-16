using Medital_Application.Responses;

namespace Medital_Application.Services.Interfaces;

public interface IPrintService
{
    Task PrintBillAsync(SaleResponse sale);
    Task PrintPurchaseAsync(PurchaseResponse purchase);
    Task PrintReportAsync(string reportTitle, object data);
    Task<byte[]> GenerateBillPdfAsync(SaleResponse sale);
}
