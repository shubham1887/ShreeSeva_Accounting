using Medital_Application.Models;

namespace Medital_Application.Responses;

public class LoginResponse
{
    public int UserId { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public UserRight? Rights { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}
