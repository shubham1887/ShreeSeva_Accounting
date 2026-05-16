namespace Medital_Application.Helpers;

public static class WhatsAppHelper
{
    public static string FormatMobile(string mobile)
    {
        var cleaned = mobile.Replace(" ", "").Replace("-", "").Replace("+", "");
        if (cleaned.StartsWith("91") && cleaned.Length == 12)
            return cleaned;
        if (cleaned.Length == 10)
            return "91" + cleaned;
        return cleaned;
    }

    public static string FormatReminderMessage(string customerName, decimal outstanding, string companyName)
        => $"Dear {customerName},\n\nYour outstanding balance is *₹{outstanding:N2}*.\nKindly clear your dues at the earliest.\n\n_{companyName}_";
}
