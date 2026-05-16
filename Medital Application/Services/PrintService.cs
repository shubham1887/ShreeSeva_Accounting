using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Medital_Application.Services;

public class PrintService : IPrintService
{
    private readonly ISettingsService _settings;

    public PrintService(ISettingsService settings) => _settings = settings;

    private static string Center(string text, int width)
    {
        if (text.Length >= width) return text;
        var padding = (width - text.Length) / 2;
        return text.PadLeft(text.Length + padding).PadRight(width);
    }

    private static string Line(int width) => new string('-', width);

    public async Task PrintBillAsync(SaleResponse sale)
    {
        var company = await _settings.GetCompanyAsync();
        var text = Helpers.PrintHelper.FormatBillText(sale,
            company?.CompanyName ?? "Medical Billing ERP",
            company?.Address1 ?? "");
        await PrintTextAsync(text);
    }

    public async Task PrintPurchaseAsync(PurchaseResponse purchase)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Center("PURCHASE RECEIPT", 48));
        sb.AppendLine(Line(48));
        sb.AppendLine($"Purchase: {purchase.VoucherNo}  Invoice: {purchase.BillNo}");
        sb.AppendLine($"Date: {purchase.VoucherDate:dd-MMM-yyyy}  Party: {purchase.AccountName}");
        sb.AppendLine(Line(48));
        sb.AppendLine($"{"Net Amount:",-20} {purchase.NetAmount,18:F2}");
        await PrintTextAsync(sb.ToString());
    }

    public async Task PrintReportAsync(string reportTitle, object data)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Center(reportTitle, 80));
        sb.AppendLine(Center($"Printed: {DateTime.Now:dd-MMM-yyyy HH:mm}", 80));
        sb.AppendLine(new string('-', 80));
        sb.AppendLine(data?.ToString() ?? "No data");
        await PrintTextAsync(sb.ToString());
    }

    public async Task<byte[]> GenerateBillPdfAsync(SaleResponse sale)
    {
        var company = await _settings.GetCompanyAsync();
        var text = Helpers.PrintHelper.FormatBillText(sale,
            company?.CompanyName ?? "Medical Billing ERP",
            company?.Address1 ?? "");
        return Encoding.UTF8.GetBytes(text);
    }

    private static async Task PrintTextAsync(string text)
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"bill_{DateTime.Now:yyyyMMddHHmmss}.txt");
        await File.WriteAllTextAsync(tmpFile, text, Encoding.UTF8);
        try
        {
            var psi = new ProcessStartInfo("notepad.exe", tmpFile) { UseShellExecute = true };
            Process.Start(psi);
        }
        catch
        {
            // Silently fail if notepad not available
        }
    }
}
