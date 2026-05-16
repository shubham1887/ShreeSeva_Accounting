namespace Medital_Application.Models;

public class WhatsAppLog
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string Mobile { get; set; } = string.Empty;
    public string MessageType { get; set; } = "BILL"; // BILL/REMINDER/CUSTOM
    public string? MessageText { get; set; }
    public string? VoucherNo { get; set; }
    public int? AccountId { get; set; }
    public string? SentAt { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? AccountName { get; set; }
}
