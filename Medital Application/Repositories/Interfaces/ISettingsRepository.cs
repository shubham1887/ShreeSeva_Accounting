using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface ISettingsRepository
{
    Task<string?> GetValueAsync(string key);
    Task<bool> SetValueAsync(string key, string value, string category = "GENERAL");
    Task<Dictionary<string, string>> GetByCategoryAsync(string category);
    Task<Company?> GetCompanyProfileAsync();
    Task<bool> SaveCompanyProfileAsync(Company company);
    Task<VoucherSeries?> GetVoucherSeriesAsync(string voucherType, string financialYear);
    Task<string> GetNextVoucherNoAsync(string voucherType, string financialYear);
}
