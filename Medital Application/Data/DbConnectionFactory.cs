using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Medital_Application.Data;

public interface IDbConnectionFactory
{
    SqliteConnection CreateConnection();
    int TenantId { get; }
    string FinancialYear { get; }
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public int TenantId { get; }
    public string FinancialYear { get; }

    public DbConnectionFactory(IConfiguration configuration)
    {
        var dataPath = configuration["Database:DataPath"] ?? "Data";
        var dbName = configuration["Database:DatabaseName"] ?? "MedicalApp.db";

        // Resolve relative to the application base directory
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var fullDataPath = System.IO.Path.IsPathRooted(dataPath)
            ? dataPath
            : System.IO.Path.Combine(appDir, dataPath);

        System.IO.Directory.CreateDirectory(fullDataPath);
        var dbPath = System.IO.Path.Combine(fullDataPath, dbName);

        _connectionString = $"Data Source={dbPath};Foreign Keys=True;";

        TenantId = int.TryParse(configuration["Tenant:TenantId"], out var tid) ? tid : 1;
        FinancialYear = configuration["Financial:CurrentFinancialYear"] ?? "2425";
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        return conn;
    }
}
