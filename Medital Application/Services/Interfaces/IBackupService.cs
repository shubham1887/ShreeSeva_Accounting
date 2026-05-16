namespace Medital_Application.Services.Interfaces;

public interface IBackupService
{
    Task<bool> CreateBackupAsync(string? customPath = null);
    Task<bool> RestoreBackupAsync(string backupFilePath);
    Task AutoBackupIfDueAsync();
    string GetDefaultBackupPath();
    List<string> GetAvailableBackups();
}
