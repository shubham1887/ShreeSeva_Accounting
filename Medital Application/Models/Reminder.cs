namespace Medital_Application.Models;

public class Reminder
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ReminderDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public bool IsCompleted { get; set; }
    public string Priority { get; set; } = "MEDIUM"; // LOW/MEDIUM/HIGH
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsDue => !IsCompleted &&
        DateTime.TryParse(ReminderDate, out var d) && d <= DateTime.Today;
}
