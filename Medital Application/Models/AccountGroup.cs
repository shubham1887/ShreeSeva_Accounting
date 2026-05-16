namespace Medital_Application.Models;

public class AccountGroup
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int? ParentGroupId { get; set; }
    public string NatureType { get; set; } = "NA"; // ASSET/LIABILITY/INCOME/EXPENSE
    public bool IsSystem { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    public string? ParentGroupName { get; set; }
}
