using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByCodeAsync(string userCode);
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();
    Task<int> CreateAsync(User user, UserRight rights);
    Task<bool> UpdateAsync(User user);
    Task<bool> UpdatePasswordAsync(int userId, string newPasswordHash);
    Task<bool> UpdateRightsAsync(UserRight rights);
    Task<UserRight?> GetRightsAsync(int userId);
    Task<bool> ValidatePasswordAsync(string userCode, string passwordHash);
}
