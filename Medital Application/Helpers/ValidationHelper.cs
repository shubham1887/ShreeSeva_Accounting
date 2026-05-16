using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Medital_Application.Helpers;

public static class ValidationHelper
{
    public static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    public static bool IsValidGSTIN(string? gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin)) return false;
        return Regex.IsMatch(gstin, @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$");
    }

    public static bool IsValidMobile(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile)) return false;
        var cleaned = mobile.Replace(" ", "").Replace("-", "");
        return Regex.IsMatch(cleaned, @"^[6-9]\d{9}$");
    }

    public static bool IsValidPAN(string? pan)
    {
        if (string.IsNullOrWhiteSpace(pan)) return false;
        return Regex.IsMatch(pan, @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$");
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    public static bool IsValidExpiryMY(string? expiry)
    {
        if (string.IsNullOrWhiteSpace(expiry)) return false;
        var parts = expiry.Split('/');
        return parts.Length == 2
               && int.TryParse(parts[0], out var m) && m >= 1 && m <= 12
               && int.TryParse(parts[1], out var y) && y >= 2020 && y <= 2099;
    }

    public static string FormatAmount(decimal amount) => $"₹{amount:N2}";
}
