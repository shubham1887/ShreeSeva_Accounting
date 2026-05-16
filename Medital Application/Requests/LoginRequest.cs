namespace Medital_Application.Requests;

public class LoginRequest
{
    public string UserCode { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TenantId { get; set; } = 1;
}
