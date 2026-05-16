using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface IJournalRepository
{
    Task<int> CreateAsync(JournalVoucher journal);
    Task<JournalVoucher?> GetByIdAsync(int id);
    Task<List<JournalVoucher>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<string> GetNextVoucherNoAsync(string financialYear);
    Task<bool> CancelAsync(int id);
}
