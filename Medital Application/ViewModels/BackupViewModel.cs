using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Medital_Application.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Medital_Application.ViewModels;

public partial class BackupViewModel : ObservableObject
{
    private readonly IBackupService _backupService;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _backupPath = string.Empty;

    public ObservableCollection<string> AvailableBackups { get; } = new();

    public BackupViewModel(IBackupService backupService)
    {
        _backupService = backupService;
        BackupPath = _backupService.GetDefaultBackupPath();
    }

    [RelayCommand]
    public void LoadBackups()
    {
        AvailableBackups.Clear();
        foreach (var b in _backupService.GetAvailableBackups())
            AvailableBackups.Add(b);
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        IsBusy = true;
        StatusMessage = "Creating backup...";
        var ok = await _backupService.CreateBackupAsync();
        StatusMessage = ok ? "Backup created successfully." : "Backup failed.";
        LoadBackups();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task RestoreBackupAsync(string filePath)
    {
        IsBusy = true;
        StatusMessage = "Restoring backup...";
        var ok = await _backupService.RestoreBackupAsync(filePath);
        StatusMessage = ok ? "Restore completed. Restart the application." : "Restore failed.";
        IsBusy = false;
    }
}
