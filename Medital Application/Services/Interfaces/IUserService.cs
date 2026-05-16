using Medital_Application.Models;
using Medital_Application.Requests;
using Medital_Application.Responses;

namespace Medital_Application.Services.Interfaces;

public interface IUserService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<bool> CheckPermissionAsync(int userId, string permission);
    Task<ApiResponse> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<User?> GetCurrentUserAsync(int userId);
    Task<List<User>> GetAllUsersAsync();
    Task<ApiResponse<int>> CreateUserAsync(User user, UserRight rights);
    Task<ApiResponse> UpdateUserAsync(User user);
    Task<ApiResponse> UpdateRightsAsync(UserRight rights);
}
