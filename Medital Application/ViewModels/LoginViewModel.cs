using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Requests;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;
using System.Windows;

namespace Medital_Application.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IUserService _userService;

    [ObservableProperty] private string _userCode = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _companyName = "Shree Seva Medical";

    public event EventHandler<LoginResponse>? LoginSuccessful;

    public LoginViewModel(IUserService userService)
    {
        _userService = userService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(UserCode) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter username and password.";
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _userService.LoginAsync(new LoginRequest
            {
                UserCode = UserCode.Trim(),
                Password = Password,
            });

            if (result.Success)
            {
                LoginSuccessful?.Invoke(this, result.Data!);
            }
            else
            {
                ErrorMessage = result.Message;
                Password = string.Empty;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearError() => ErrorMessage = string.Empty;
}
