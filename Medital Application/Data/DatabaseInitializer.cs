using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Medital_Application.Data;

public class DatabaseInitializer
{
    private readonly IDbConnectionFactory _factory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IDbConnectionFactory factory, ILogger<DatabaseInitializer> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing database...");

            // Find schema.sql
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var schemaPath = Path.Combine(baseDir, "Data", "schema.sql");

            if (!File.Exists(schemaPath))
            {
                _logger.LogError("schema.sql not found at {Path}", schemaPath);
                throw new FileNotFoundException("schema.sql not found", schemaPath);
            }

            var schemaSql = await File.ReadAllTextAsync(schemaPath);

            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Enable WAL and foreign keys
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
                await pragmaCmd.ExecuteNonQueryAsync();
            }

            // Split and execute schema statements
            var statements = SplitSqlStatements(schemaSql);
            foreach (var stmt in statements)
            {
                if (string.IsNullOrWhiteSpace(stmt)) continue;
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = stmt;
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && stmt.TrimStart().StartsWith("--"))
                {
                    // Skip comment-only statements
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Statement warning: {Stmt}", stmt[..Math.Min(100, stmt.Length)]);
                }
            }

            _logger.LogInformation("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed");
            throw;
        }
    }

    private static List<string> SplitSqlStatements(string sql)
    {
        var statements = new List<string>();
        var current = new System.Text.StringBuilder();
        var lines = sql.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.TrimEnd();

            // Skip pure comment lines for processing but include them in statement context
            if (trimmed.StartsWith("--")) continue;

            current.AppendLine(trimmed);

            // A statement ends with semicolon
            if (trimmed.TrimEnd().EndsWith(';'))
            {
                var stmt = current.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(stmt))
                    statements.Add(stmt);
                current.Clear();
            }
        }

        // Handle remaining
        var remaining = current.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(remaining))
            statements.Add(remaining);

        return statements;
    }

    /// <summary>Verify DB is accessible and has expected tables.</summary>
    public async Task<bool> VerifyAsync()
    {
        try
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Products'";
            var result = await cmd.ExecuteScalarAsync();
            return result != null && Convert.ToInt64(result) > 0;
        }
        catch
        {
            return false;
        }
    }
}
