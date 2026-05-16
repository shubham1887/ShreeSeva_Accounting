using System.IO;
using System.Text;

namespace Medital_Application.Helpers;

/// <summary>Simple CSV exporter (Excel-compatible).</summary>
public static class ExcelExporter
{
    public static string ExportToCsv<T>(IEnumerable<T> data, IEnumerable<string> headers, Func<T, IEnumerable<object?>> rowSelector)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var item in data)
            sb.AppendLine(string.Join(",", rowSelector(item).Select(v => EscapeCsv(v?.ToString() ?? ""))));
        return sb.ToString();
    }

    public static async Task SaveCsvAsync(string filePath, string csvContent)
    {
        await File.WriteAllTextAsync(filePath, csvContent, Encoding.UTF8);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
