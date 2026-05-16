namespace Medital_Application.Models;

public class User
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string UserCode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? JoinDate { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public UserRight? Rights { get; set; }
}
