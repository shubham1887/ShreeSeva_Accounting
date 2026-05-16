using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class JournalRepository : IJournalRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ISettingsRepository _settings;

    public JournalRepository(IDbConnectionFactory db, ISettingsRepository settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<int> CreateAsync(JournalVoucher journal)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO JournalVouchers
                (TenantId, VoucherNo, VoucherDate, AccountId, DebitAmount, CreditAmount,
                 Narration, FinancialYear, IsDeleted, CreatedAt, UpdatedAt)
            VALUES
                (@tid, @vno, @dt, @aid, @dr, @cr,
                 @narr, @fy, 0, @now, @now);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@vno", journal.VoucherNo);
        cmd.Parameters.AddWithValue("@dt", journal.VoucherDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@aid", journal.AccountId);
        cmd.Parameters.AddWithValue("@dr", journal.DebitAmount);
        cmd.Parameters.AddWithValue("@cr", journal.CreditAmount);
        cmd.Parameters.AddWithValue("@narr", (object?)journal.Narration ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@fy", journal.FinancialYear);
        cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<JournalVoucher?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT j.*, a.AccountName
            FROM JournalVouchers j
            LEFT JOIN Accounts a ON j.AccountId = a.Id
            WHERE j.Id = @id AND j.TenantId = @tid AND j.IsDeleted = 0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapJournal(reader);
        return null;
    }

    public async Task<List<JournalVoucher>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT j.*, a.AccountName
            FROM JournalVouchers j
            LEFT JOIN Accounts a ON j.AccountId = a.Id
            WHERE j.TenantId = @tid AND j.IsDeleted = 0
              AND j.VoucherDate BETWEEN @from AND @to
            ORDER BY j.VoucherDate DESC, j.Id DESC";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));
        var list = new List<JournalVoucher>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapJournal(reader));
        return list;
    }

    public Task<string> GetNextVoucherNoAsync(string financialYear)
        => _settings.GetNextVoucherNoAsync("JV", financialYear);

    public async Task<bool> CancelAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE JournalVouchers SET IsDeleted=1, UpdatedAt=@now WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static JournalVoucher MapJournal(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
        VoucherNo = r.GetString(r.GetOrdinal("VoucherNo")),
        VoucherDate = DateTime.Parse(r.GetString(r.GetOrdinal("VoucherDate"))),
        AccountId = r.GetInt32(r.GetOrdinal("AccountId")),
        AccountName = r.IsDBNull(r.GetOrdinal("AccountName")) ? null : r.GetString(r.GetOrdinal("AccountName")),
        DebitAmount = r.GetDecimal(r.GetOrdinal("DebitAmount")),
        CreditAmount = r.GetDecimal(r.GetOrdinal("CreditAmount")),
        Narration = r.IsDBNull(r.GetOrdinal("Narration")) ? null : r.GetString(r.GetOrdinal("Narration")),
        FinancialYear = r.GetString(r.GetOrdinal("FinancialYear")),
    };
}
