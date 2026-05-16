using Medital_Application.Models;
using Medital_Application.Requests;

namespace Medital_Application.Repositories.Interfaces;

public interface ISaleRepository
{
    Task<int> CreateAsync(SaleMaster master, List<SaleDetail> details);
    Task<SaleMaster?> GetByIdAsync(int id);
    Task<SaleMaster?> GetByVoucherNoAsync(string voucherNo);
    Task<List<SaleMaster>> GetByDateRangeAsync(DateTime from, DateTime to, int? accountId = null);
    Task<List<SaleDetail>> GetDetailsAsync(int saleMasterId);
    Task<string> GetNextVoucherNoAsync(string financialYear);
    Task<decimal> GetTotalByDateAsync(DateTime date);
    Task<decimal> GetTotalByMonthAsync(int year, int month);
    Task<bool> CancelAsync(int id);
}
