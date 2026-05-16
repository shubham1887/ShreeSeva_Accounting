using Medital_Application.Responses;

namespace Medital_Application.Services.Interfaces;

public interface IWhatsAppService
{
    Task<bool> SendBillAsync(SaleResponse sale, string mobile);
    Task<bool> SendPaymentReminderAsync(int accountId, decimal outstanding);
    Task<bool> SendCustomMessageAsync(string mobile, string message);
    bool IsEnabled { get; }
}
