using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class AccountsViewModel : ObservableObject
{
    private readonly IAccountRepository _accountRepo;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private Account? _selectedAccount;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<Account> Accounts { get; } = new();
    public ObservableCollection<AccountGroup> Groups { get; } = new();

    public AccountsViewModel(IAccountRepository accountRepo) => _accountRepo = accountRepo;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var accounts = await _accountRepo.SearchAsync(
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);
            Accounts.Clear();
            foreach (var a in accounts) Accounts.Add(a);
            var groups = await _accountRepo.GetGroupsAsync();
            Groups.Clear();
            foreach (var g in groups) Groups.Add(g);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SearchAsync() => await LoadAsync();

    [RelayCommand]
    private async Task DeleteAsync(Account account)
    {
        await _accountRepo.DeleteAsync(account.Id);
        await LoadAsync();
    }
}
