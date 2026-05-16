namespace Medital_Application.Models;

public class AppSetting
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public string Category { get; set; } = "GENERAL";
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
