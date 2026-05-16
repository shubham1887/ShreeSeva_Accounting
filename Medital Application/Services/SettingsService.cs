using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Medital_Application.Services;

public class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _settingsRepo;
    private readonly IConfiguration _config;

    public SettingsService(ISettingsRepository settingsRepo, IConfiguration config)
    {
        _settingsRepo = settingsRepo;
        _config = config;
    }

    public Task<string?> GetAsync(string key) => _settingsRepo.GetValueAsync(key);
    public Task SetAsync(string key, string value, string category = "GENERAL") =>
        _settingsRepo.SetValueAsync(key, value, category);
    public Task<Company?> GetCompanyAsync() => _settingsRepo.GetCompanyProfileAsync();
    public Task SaveCompanyAsync(Company company) => _settingsRepo.SaveCompanyProfileAsync(company);
    public Task<string> GetFinancialYearAsync() =>
        Task.FromResult(_config["Financial:CurrentFinancialYear"] ?? "2425");
    public Task<bool> IsGSTRegisteredAsync() =>
        Task.FromResult(_config["Financial:GSTRegistered"] == "true");
    public Task<string> GetStateCodeAsync() =>
        Task.FromResult(_config["Company:StateCode"] ?? "27");
}
