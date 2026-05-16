using Medital_Application.Helpers;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Requests;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Medital_Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;

    public UserService(IUserRepository userRepo, IConfiguration config)
    {
        _userRepo = userRepo;
        _config = config;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserCode) || string.IsNullOrWhiteSpace(request.Password))
            return ApiResponse<LoginResponse>.Fail("Username and password are required.");

        var hash = ValidationHelper.HashPassword(request.Password);
        var valid = await _userRepo.ValidatePasswordAsync(request.UserCode.ToUpper(), hash);
        if (!valid)
            return ApiResponse<LoginResponse>.Fail("Invalid username or password.");

        var user = await _userRepo.GetByCodeAsync(request.UserCode.ToUpper());
        if (user == null || !user.IsActive)
            return ApiResponse<LoginResponse>.Fail("Account is inactive.");

        var rights = await _userRepo.GetRightsAsync(user.Id);
        var fy = _config["Financial:CurrentFinancialYear"] ?? "2425";
        var companyName = _config["Company:Name"] ?? "Medical App";

        return ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            UserId = user.Id,
            UserCode = user.UserCode,
            UserName = user.UserName,
            IsAdmin = user.IsAdmin,
            Rights = rights,
            FinancialYear = fy,
            CompanyName = companyName,
        });
    }

    public async Task<bool> CheckPermissionAsync(int userId, string permission)
    {
        if (userId == 0) return false;
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return false;
        if (user.IsAdmin) return true;

        var rights = await _userRepo.GetRightsAsync(userId);
        if (rights == null) return false;

        return permission switch
        {
            "CanSale" => rights.CanSale,
            "CanSaleEdit" => rights.CanSaleEdit,
            "CanSaleDelete" => rights.CanSaleDelete,
            "CanPurchase" => rights.CanPurchase,
            "CanPurchaseEdit" => rights.CanPurchaseEdit,
            "CanReceipt" => rights.CanReceipt,
            "CanPayment" => rights.CanPayment,
            "CanCreditNote" => rights.CanCreditNote,
            "CanDebitNote" => rights.CanDebitNote,
            "CanJournal" => rights.CanJournal,
            "CanReports" => rights.CanReports,
            "CanGSTReports" => rights.CanGSTReports,
            "CanUserMgmt" => rights.CanUserMgmt,
            "CanSettings" => rights.CanSettings,
            "CanBackup" => rights.CanBackup,
            "CanViewCost" => rights.CanViewCost,
            "CanGiveDiscount" => rights.CanGiveDiscount,
            _ => false,
        };
    }

    public async Task<ApiResponse> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            return ApiResponse.Fail("New password must be at least 4 characters.");

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return ApiResponse.Fail("User not found.");

        var oldHash = ValidationHelper.HashPassword(oldPassword);
        if (user.PasswordHash != oldHash)
            return ApiResponse.Fail("Current password is incorrect.");

        var newHash = ValidationHelper.HashPassword(newPassword);
        await _userRepo.UpdatePasswordAsync(userId, newHash);
        return ApiResponse.Ok("Password changed successfully.");
    }

    public Task<User?> GetCurrentUserAsync(int userId) => _userRepo.GetByIdAsync(userId);

    public Task<List<User>> GetAllUsersAsync() => _userRepo.GetAllAsync();

    public async Task<ApiResponse<int>> CreateUserAsync(User user, UserRight rights)
    {
        if (string.IsNullOrWhiteSpace(user.UserCode))
            return ApiResponse<int>.Fail("User code is required.");
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            return ApiResponse<int>.Fail("Password is required.");
        user.PasswordHash = ValidationHelper.HashPassword(user.PasswordHash);
        user.UserCode = user.UserCode.ToUpper().Trim();
        var id = await _userRepo.CreateAsync(user, rights);
        return ApiResponse<int>.Ok(id, "User created successfully.");
    }

    public async Task<ApiResponse> UpdateUserAsync(User user)
    {
        var ok = await _userRepo.UpdateAsync(user);
        return ok ? ApiResponse.Ok("User updated.") : ApiResponse.Fail("Update failed.");
    }

    public async Task<ApiResponse> UpdateRightsAsync(UserRight rights)
    {
        var ok = await _userRepo.UpdateRightsAsync(rights);
        return ok ? ApiResponse.Ok("Rights updated.") : ApiResponse.Fail("Update failed.");
    }
}
