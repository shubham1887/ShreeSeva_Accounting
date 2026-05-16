using Medital_Application.Data;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Medital_Application.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly IConfiguration _config;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ILogger<WhatsAppService> _logger;
    private static readonly HttpClient _httpClient = new();

    public bool IsEnabled => _config["WhatsApp:Enabled"] == "true"
                              && !string.IsNullOrEmpty(_config["WhatsApp:ApiUrl"]);

    public WhatsAppService(IConfiguration config, IDbConnectionFactory dbFactory, ILogger<WhatsAppService> logger)
    {
        _config = config;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<bool> SendBillAsync(SaleResponse sale, string mobile)
    {
        if (!IsEnabled) return false;
        var message = FormatBillMessage(sale);
        return await SendAsync(mobile, message, "BILL", sale.VoucherNo);
    }

    public async Task<bool> SendPaymentReminderAsync(int accountId, decimal outstanding)
    {
        if (!IsEnabled) return false;
        var message = $"Dear Customer, your outstanding balance is ₹{outstanding:N2}. Please make payment at the earliest. - {_config["Company:Name"]}";
        return await SendAsync("", message, "REMINDER", null, accountId);
    }

    public async Task<bool> SendCustomMessageAsync(string mobile, string message)
    {
        if (!IsEnabled) return false;
        return await SendAsync(mobile, message, "CUSTOM", null);
    }

    private async Task<bool> SendAsync(string mobile, string message, string type, string? voucherNo = null, int? accountId = null)
    {
        bool success = false;
        string? error = null;
        try
        {
            var apiUrl = _config["WhatsApp:ApiUrl"]!;
            var apiKey = _config["WhatsApp:ApiKey"] ?? "";
            var payload = new { phone = mobile, message, apikey = apiKey };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(apiUrl, content);
            success = response.IsSuccessStatusCode;
            if (!success) error = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _logger.LogError(ex, "WhatsApp API call failed");
        }

        await LogMessageAsync(mobile, type, message, voucherNo, accountId, success, error);
        return success;
    }

    private async Task LogMessageAsync(string mobile, string type, string? message, string? voucherNo, int? accountId, bool success, string? error)
    {
        try
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO WhatsAppLogs(TenantId,Mobile,MessageType,MessageText,VoucherNo,AccountId,SentAt,IsSuccess,ErrorMessage,CreatedAt)
                VALUES(@tid,@mob,@type,@msg,@vno,@aid,@sent,@ok,@err,datetime('now'))";
            cmd.Parameters.AddWithValue("@tid", _dbFactory.TenantId);
            cmd.Parameters.AddWithValue("@mob", mobile);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@msg", message ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@vno", voucherNo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@aid", accountId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@sent", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@ok", success ? 1 : 0);
            cmd.Parameters.AddWithValue("@err", error ?? (object)DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log WhatsApp message");
        }
    }

    private string FormatBillMessage(SaleResponse sale)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"*{_config["Company:Name"]}*");
        sb.AppendLine($"Bill No: {sale.VoucherNo}");
        sb.AppendLine($"Date: {sale.VoucherDate:dd-MMM-yyyy}");
        sb.AppendLine($"Customer: {sale.AccountName}");
        sb.AppendLine("─────────────────────");
        foreach (var item in sale.Items)
            sb.AppendLine($"{item.ProductName} × {item.Quantity} = ₹{item.LineTotal:N2}");
        sb.AppendLine("─────────────────────");
        if (sale.CashDiscAmount > 0)
            sb.AppendLine($"Discount: ₹{sale.CashDiscAmount:N2}");
        sb.AppendLine($"GST: ₹{(sale.TotalSGST + sale.TotalCGST + sale.TotalIGST):N2}");
        sb.AppendLine($"*Total: ₹{sale.NetAmount:N2}*");
        sb.AppendLine($"Payment: {sale.PaymentMode}");
        sb.AppendLine("Thank you for your purchase!");
        return sb.ToString();
    }
}
