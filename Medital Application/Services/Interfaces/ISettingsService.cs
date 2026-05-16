using Medital_Application.Models;

namespace Medital_Application.Services.Interfaces;

public interface ISettingsService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value, string category = "GENERAL");
    Task<Company?> GetCompanyAsync();
    Task SaveCompanyAsync(Company company);
    Task<string> GetFinancialYearAsync();
    Task<bool> IsGSTRegisteredAsync();
    Task<string> GetStateCodeAsync();
}
