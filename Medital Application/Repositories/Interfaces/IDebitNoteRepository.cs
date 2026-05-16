using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface IDebitNoteRepository
{
    Task<int> CreateAsync(DebitNoteMaster master, List<DebitNoteDetail> details);
    Task<DebitNoteMaster?> GetByIdAsync(int id);
    Task<List<DebitNoteMaster>> GetByDateRangeAsync(DateTime from, DateTime to, int? accountId = null);
    Task<List<DebitNoteDetail>> GetDetailsAsync(int masterId);
    Task<string> GetNextVoucherNoAsync(string financialYear);
}
