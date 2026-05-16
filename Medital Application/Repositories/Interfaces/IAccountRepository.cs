using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface IAccountRepository
{
    Task<List<Account>> SearchAsync(string? searchTerm, int? groupId = null);
    Task<Account?> GetByIdAsync(int id);
    Task<int> CreateAsync(Account account);
    Task<bool> UpdateAsync(Account account);
    Task<bool> DeleteAsync(int id);
    Task<List<Account>> GetDistributorsAsync();
    Task<List<Account>> GetCustomersAsync();
    Task<List<Account>> GetByGroupCodeAsync(string groupCode);
    Task<List<AccountGroup>> GetGroupsAsync();
    Task<decimal> GetOutstandingBalanceAsync(int accountId);
}
