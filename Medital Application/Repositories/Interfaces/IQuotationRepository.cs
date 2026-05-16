using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface IQuotationRepository
{
    Task<int> CreateAsync(Quotation quotation, List<QuotationDetail> details);
    Task<Quotation?> GetByIdAsync(int id);
    Task<List<Quotation>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<string> GetNextVoucherNoAsync(string financialYear);
    Task<bool> MarkConvertedAsync(int id, string saleVoucherNo);
}
