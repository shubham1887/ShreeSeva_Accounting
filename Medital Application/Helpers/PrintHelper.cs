using Medital_Application.Responses;
using System.Text;

namespace Medital_Application.Helpers;

/// <summary>Utility for formatting bill text for printing.</summary>
public static class PrintHelper
{
    public static string FormatBillText(SaleResponse sale, string companyName, string address)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Center(companyName, 48));
        sb.AppendLine(Center(address, 48));
        sb.AppendLine(new string('-', 48));
        sb.AppendLine($"Bill No  : {sale.VoucherNo}");
        sb.AppendLine($"Date     : {sale.VoucherDate:dd-MMM-yyyy}");
        sb.AppendLine($"Customer : {sale.AccountName}");
        sb.AppendLine(new string('-', 48));
        sb.AppendLine($"{"Product",-22} {"Qty",5} {"Rate",8} {"Amt",10}");
        sb.AppendLine(new string('-', 48));
        foreach (var item in sale.Items)
            sb.AppendLine($"{item.ProductName[..Math.Min(22, item.ProductName.Length)],22} {item.Quantity,5} {item.SaleRate,8:N2} {item.LineTotal,10:N2}");
        sb.AppendLine(new string('-', 48));
        sb.AppendLine($"{"Gross Amount",-35} {sale.GrossAmount,12:N2}");
        if (sale.CashDiscAmount > 0)
            sb.AppendLine($"{"Discount",-35} {-sale.CashDiscAmount,12:N2}");
        var gst = sale.TotalSGST + sale.TotalCGST + sale.TotalIGST;
        if (gst > 0)
            sb.AppendLine($"{"GST",-35} {gst,12:N2}");
        if (sale.RoundOff != 0)
            sb.AppendLine($"{"Round Off",-35} {sale.RoundOff,12:N2}");
        sb.AppendLine(new string('=', 48));
        sb.AppendLine($"{"NET AMOUNT",-35} {sale.NetAmount,12:N2}");
        sb.AppendLine(new string('=', 48));
        sb.AppendLine($"Amount: {sale.AmountInWords}");
        sb.AppendLine($"Payment: {sale.PaymentMode}");
        sb.AppendLine(new string('-', 48));
        sb.AppendLine(Center("Thank you! Visit Again", 48));
        return sb.ToString();
    }

    private static string Center(string text, int width)
    {
        if (text.Length >= width) return text;
        var padding = (width - text.Length) / 2;
        return text.PadLeft(text.Length + padding).PadRight(width);
    }
}
