using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface ICreditNoteRepository
{
    Task<int> CreateAsync(CreditNoteMaster master, List<CreditNoteDetail> details);
    Task<CreditNoteMaster?> GetByIdAsync(int id);
    Task<List<CreditNoteMaster>> GetByDateRangeAsync(DateTime from, DateTime to, int? accountId = null);
    Task<List<CreditNoteDetail>> GetDetailsAsync(int masterId);
    Task<string> GetNextVoucherNoAsync(string financialYear);
}
