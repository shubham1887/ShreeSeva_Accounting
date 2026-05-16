using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface IReceiptRepository
{
    Task<int> CreateAsync(ReceiptMaster master, List<ReceiptDetail> details);
    Task<ReceiptMaster?> GetByIdAsync(int id);
    Task<List<ReceiptMaster>> GetByAccountAsync(int accountId);
    Task<List<ReceiptMaster>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<string> GetNextVoucherNoAsync(string financialYear);
    Task<decimal> GetTotalPendingByAccountAsync(int accountId);
}
