using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task<int> CreateAsync(PaymentMaster master, List<PaymentDetail> details);
    Task<PaymentMaster?> GetByIdAsync(int id);
    Task<List<PaymentMaster>> GetByAccountAsync(int accountId);
    Task<List<PaymentMaster>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<string> GetNextVoucherNoAsync(string financialYear);
    Task<decimal> GetTotalPendingByAccountAsync(int accountId);
}
