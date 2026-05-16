using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class ReceiptRepository : IReceiptRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ISettingsRepository _settings;

    public ReceiptRepository(IDbConnectionFactory db, ISettingsRepository settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<int> CreateAsync(ReceiptMaster master, List<ReceiptDetail> details)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO ReceiptMaster
                    (TenantId, VoucherNo, VoucherDate, AccountId, Amount, ChequeNo, ChequeDate,
                     PaymentMode, Narration, FinancialYear, IsDeleted, CreatedAt, UpdatedAt)
                VALUES
                    (@tid, @vno, @dt, @aid, @amt, @chno, @chdt,
                     @mode, @narr, @fy, 0, @now, @now);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@tid", _db.TenantId);
            cmd.Parameters.AddWithValue("@vno", master.VoucherNo);
            cmd.Parameters.AddWithValue("@dt", master.VoucherDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@aid", master.AccountId);
            cmd.Parameters.AddWithValue("@amt", master.Amount);
            cmd.Parameters.AddWithValue("@chno", (object?)master.ChequeNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@chdt", master.ChequeDate.HasValue ? master.ChequeDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);
            cmd.Parameters.AddWithValue("@mode", master.PaymentMode);
            cmd.Parameters.AddWithValue("@narr", (object?)master.Narration ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fy", master.FinancialYear);
            cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            var masterId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            foreach (var d in details)
            {
                var dc = conn.CreateCommand();
                dc.Transaction = tx;
                dc.CommandText = @"
                    INSERT INTO ReceiptDetails
                        (TenantId, ReceiptMasterId, SaleMasterId, AllocatedAmount, IsDeleted, CreatedAt)
                    VALUES
                        (@tid, @rid, @sid, @alloc, 0, @now)";
                dc.Parameters.AddWithValue("@tid", _db.TenantId);
                dc.Parameters.AddWithValue("@rid", masterId);
                dc.Parameters.AddWithValue("@sid", (object?)d.SaleMasterId ?? DBNull.Value);
                dc.Parameters.AddWithValue("@alloc", d.AllocatedAmount);
                dc.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                await dc.ExecuteNonQueryAsync();
            }

            tx.Commit();
            return masterId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<ReceiptMaster?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT r.*, a.AccountName
            FROM ReceiptMaster r
            LEFT JOIN Accounts a ON r.AccountId = a.Id
            WHERE r.Id = @id AND r.TenantId = @tid AND r.IsDeleted = 0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapMaster(reader);
        return null;
    }

    public async Task<List<ReceiptMaster>> GetByAccountAsync(int accountId)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT r.*, a.AccountName
            FROM ReceiptMaster r
            LEFT JOIN Accounts a ON r.AccountId = a.Id
            WHERE r.AccountId = @aid AND r.TenantId = @tid AND r.IsDeleted = 0
            ORDER BY r.VoucherDate DESC";
        cmd.Parameters.AddWithValue("@aid", accountId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        var list = new List<ReceiptMaster>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapMaster(reader));
        return list;
    }

    public async Task<List<ReceiptMaster>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT r.*, a.AccountName
            FROM ReceiptMaster r
            LEFT JOIN Accounts a ON r.AccountId = a.Id
            WHERE r.TenantId = @tid AND r.IsDeleted = 0
              AND r.VoucherDate BETWEEN @from AND @to
            ORDER BY r.VoucherDate DESC, r.Id DESC";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));
        var list = new List<ReceiptMaster>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapMaster(reader));
        return list;
    }

    public Task<string> GetNextVoucherNoAsync(string financialYear)
        => _settings.GetNextVoucherNoAsync("RC", financialYear);

    public async Task<decimal> GetTotalPendingByAccountAsync(int accountId)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
                COALESCE((SELECT SUM(s.NetAmount) FROM SaleMaster s
                           WHERE s.AccountId = @aid AND s.TenantId = @tid AND s.IsDeleted = 0), 0) -
                COALESCE((SELECT SUM(r.Amount) FROM ReceiptMaster r
                           WHERE r.AccountId = @aid AND r.TenantId = @tid AND r.IsDeleted = 0), 0)";
        cmd.Parameters.AddWithValue("@aid", accountId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        var result = await cmd.ExecuteScalarAsync();
        return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
    }

    private static ReceiptMaster MapMaster(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
        VoucherNo = r.GetString(r.GetOrdinal("VoucherNo")),
        VoucherDate = DateTime.Parse(r.GetString(r.GetOrdinal("VoucherDate"))),
        AccountId = r.GetInt32(r.GetOrdinal("AccountId")),
        AccountName = r.IsDBNull(r.GetOrdinal("AccountName")) ? null : r.GetString(r.GetOrdinal("AccountName")),
        Amount = r.GetDecimal(r.GetOrdinal("Amount")),
        ChequeNo = r.IsDBNull(r.GetOrdinal("ChequeNo")) ? null : r.GetString(r.GetOrdinal("ChequeNo")),
        ChequeDate = r.IsDBNull(r.GetOrdinal("ChequeDate")) ? null : DateTime.Parse(r.GetString(r.GetOrdinal("ChequeDate"))),
        PaymentMode = r.GetString(r.GetOrdinal("PaymentMode")),
        Narration = r.IsDBNull(r.GetOrdinal("Narration")) ? null : r.GetString(r.GetOrdinal("Narration")),
        FinancialYear = r.GetString(r.GetOrdinal("FinancialYear")),
    };
}
