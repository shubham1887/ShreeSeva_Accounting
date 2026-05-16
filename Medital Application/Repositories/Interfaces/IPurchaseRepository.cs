using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface IPurchaseRepository
{
    Task<int> CreateAsync(PurchaseMaster master, List<PurchaseDetail> details);
    Task<PurchaseMaster?> GetByIdAsync(int id);
    Task<PurchaseMaster?> GetByVoucherNoAsync(string voucherNo);
    Task<List<PurchaseMaster>> GetByDateRangeAsync(DateTime from, DateTime to, int? accountId = null);
    Task<List<PurchaseDetail>> GetDetailsAsync(int purchaseMasterId);
    Task<string> GetNextVoucherNoAsync(string financialYear);
    Task<decimal> GetTotalByDateAsync(DateTime date);
    Task<bool> CancelAsync(int id);
    Task<List<PurchaseMaster>> GetUnpaidAsync(int accountId);
}
