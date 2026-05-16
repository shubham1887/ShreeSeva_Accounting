using Medital_Application.Data;
using Medital_Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Medital_Application.Services;

public class BackupService : IBackupService
{
    private readonly IConfiguration _config;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ILogger<BackupService> _logger;

    public BackupService(IConfiguration config, IDbConnectionFactory dbFactory, ILogger<BackupService> logger)
    {
        _config = config;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private string GetDbFilePath()
    {
        var dataPath = _config["Database:DataPath"] ?? "Data";
        var dbName = _config["Database:DatabaseName"] ?? "MedicalApp.db";
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var fullDataPath = Path.IsPathRooted(dataPath) ? dataPath : Path.Combine(appDir, dataPath);
        return Path.Combine(fullDataPath, dbName);
    }

    public string GetDefaultBackupPath()
    {
        var backupPath = _config["Database:BackupPath"] ?? "Backup";
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.IsPathRooted(backupPath) ? backupPath : Path.Combine(appDir, backupPath);
    }

    public async Task<bool> CreateBackupAsync(string? customPath = null)
    {
        try
        {
            var sourceFile = GetDbFilePath();
            if (!File.Exists(sourceFile))
            {
                _logger.LogWarning("Database file not found at {Path}", sourceFile);
                return false;
            }

            var backupDir = customPath ?? GetDefaultBackupPath();
            Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var dbName = Path.GetFileNameWithoutExtension(_config["Database:DatabaseName"] ?? "MedicalApp");
            var backupFileName = $"{dbName}_{timestamp}.db";
            var backupFilePath = Path.Combine(backupDir, backupFileName);

            // SQLite WAL checkpoint before backup
            using (var conn = _dbFactory.CreateConnection())
            {
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA wal_checkpoint(FULL);";
                await cmd.ExecuteNonQueryAsync();
            }

            File.Copy(sourceFile, backupFilePath, overwrite: false);
            _logger.LogInformation("Backup created: {Path}", backupFilePath);

            // Cleanup old backups (keep last 30)
            CleanupOldBackups(backupDir, dbName, 30);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed");
            return false;
        }
    }

    public async Task<bool> RestoreBackupAsync(string backupFilePath)
    {
        try
        {
            if (!File.Exists(backupFilePath))
                return false;

            var targetFile = GetDbFilePath();
            // Make a safety backup first
            await CreateBackupAsync();
            File.Copy(backupFilePath, targetFile, overwrite: true);
            _logger.LogInformation("Database restored from {Path}", backupFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore failed");
            return false;
        }
    }

    public async Task AutoBackupIfDueAsync()
    {
        if (_config["Database:AutoBackup"] != "true") return;
        var intervalDays = int.TryParse(_config["Database:BackupIntervalDays"], out var d) ? d : 1;
        var backupDir = GetDefaultBackupPath();
        var dbName = Path.GetFileNameWithoutExtension(_config["Database:DatabaseName"] ?? "MedicalApp");
        var lastBackup = GetAvailableBackups()
            .Where(f => Path.GetFileName(f).StartsWith(dbName))
            .OrderByDescending(f => f)
            .FirstOrDefault();

        bool isDue = true;
        if (lastBackup != null)
        {
            var lastModified = File.GetCreationTime(lastBackup);
            isDue = (DateTime.Now - lastModified).TotalDays >= intervalDays;
        }

        if (isDue)
            await CreateBackupAsync();
    }

    public List<string> GetAvailableBackups()
    {
        var backupDir = GetDefaultBackupPath();
        if (!Directory.Exists(backupDir)) return new List<string>();
        return Directory.GetFiles(backupDir, "*.db")
            .OrderByDescending(f => f)
            .ToList();
    }

    private static void CleanupOldBackups(string dir, string prefix, int keep)
    {
        var files = Directory.GetFiles(dir, $"{prefix}_*.db")
            .OrderByDescending(f => f)
            .Skip(keep)
            .ToList();
        foreach (var f in files)
        {
            try { File.Delete(f); }
            catch { /* ignore */ }
        }
    }
}
