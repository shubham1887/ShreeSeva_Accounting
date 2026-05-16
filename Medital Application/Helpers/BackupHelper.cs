using System.IO;

namespace Medital_Application.Helpers;

public static class BackupHelper
{
    public static string GenerateBackupFileName(string dbName)
    {
        var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"{Path.GetFileNameWithoutExtension(dbName)}_{ts}.db";
    }

    public static List<string> GetBackupFiles(string directory, string dbName)
    {
        if (!Directory.Exists(directory)) return new();
        var prefix = Path.GetFileNameWithoutExtension(dbName);
        return Directory.GetFiles(directory, $"{prefix}_*.db")
            .OrderByDescending(f => f)
            .ToList();
    }
}
