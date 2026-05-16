using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class UserManagementViewModel : ObservableObject
{
    private readonly IUserService _userService;

    [ObservableProperty] private User? _selectedUser;
    [ObservableProperty] private UserRight? _selectedUserRights;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSaving;

    public ObservableCollection<User> Users { get; } = new();

    public UserManagementViewModel(IUserService userService) => _userService = userService;

    [RelayCommand]
    public async Task LoadAsync()
    {
        var users = await _userService.GetAllUsersAsync();
        Users.Clear();
        foreach (var u in users) Users.Add(u);
    }

    [RelayCommand]
    private async Task SaveRightsAsync()
    {
        if (SelectedUserRights == null) return;
        IsSaving = true;
        try
        {
            var result = await _userService.UpdateRightsAsync(SelectedUserRights);
            StatusMessage = result.Message;
        }
        finally { IsSaving = false; }
    }
}
