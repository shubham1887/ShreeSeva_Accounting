using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Models;
using Medital_Application.Services.Interfaces;

namespace Medital_Application.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IBackupService _backupService;

    [ObservableProperty] private Company? _company;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSaving;

    public SettingsViewModel(ISettingsService settingsService, IBackupService backupService)
    {
        _settingsService = settingsService;
        _backupService = backupService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        Company = await _settingsService.GetCompanyAsync() ?? new Company();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Company == null) return;
        IsSaving = true;
        try
        {
            await _settingsService.SaveCompanyAsync(Company);
            StatusMessage = "Settings saved.";
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { IsSaving = false; }
    }

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        StatusMessage = "Creating backup...";
        var ok = await _backupService.CreateBackupAsync();
        StatusMessage = ok ? "Backup created." : "Backup failed.";
    }
}
