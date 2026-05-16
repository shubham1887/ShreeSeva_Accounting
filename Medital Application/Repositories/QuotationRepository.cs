using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class QuotationRepository : IQuotationRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ISettingsRepository _settings;

    public QuotationRepository(IDbConnectionFactory db, ISettingsRepository settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<int> CreateAsync(Quotation quotation, List<QuotationDetail> details)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO QuotationMaster
                    (TenantId, VoucherNo, VoucherDate, AccountId, ValidDays,
                     NetAmount, Narration, FinancialYear, IsConverted, IsDeleted, CreatedAt, UpdatedAt)
                VALUES
                    (@tid, @vno, @dt, @aid, @valid,
                     @net, @narr, @fy, 0, 0, @now, @now);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@tid", _db.TenantId);
            cmd.Parameters.AddWithValue("@vno", quotation.VoucherNo);
            cmd.Parameters.AddWithValue("@dt", quotation.VoucherDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@aid", quotation.AccountId);
            cmd.Parameters.AddWithValue("@valid", quotation.ValidDays);
            cmd.Parameters.AddWithValue("@net", quotation.NetAmount);
            cmd.Parameters.AddWithValue("@narr", (object?)quotation.Narration ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fy", quotation.FinancialYear);
            cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            var masterId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            foreach (var d in details)
            {
                var dc = conn.CreateCommand();
                dc.Transaction = tx;
                dc.CommandText = @"
                    INSERT INTO QuotationDetails
                        (TenantId, QuotationMasterId, ProductId, Quantity, SaleRate, DiscPer,
                         SGSTRate, CGSTRate, IGSTRate, LineTotal, IsDeleted, CreatedAt)
                    VALUES
                        (@tid, @qid, @pid, @qty, @rate, @disc,
                         @sgst, @cgst, @igst, @lt, 0, @now)";
                dc.Parameters.AddWithValue("@tid", _db.TenantId);
                dc.Parameters.AddWithValue("@qid", masterId);
                dc.Parameters.AddWithValue("@pid", d.ProductId);
                dc.Parameters.AddWithValue("@qty", d.Quantity);
                dc.Parameters.AddWithValue("@rate", d.SaleRate);
                dc.Parameters.AddWithValue("@disc", d.DiscPer);
                dc.Parameters.AddWithValue("@sgst", d.SGSTRate);
                dc.Parameters.AddWithValue("@cgst", d.CGSTRate);
                dc.Parameters.AddWithValue("@igst", d.IGSTRate);
                dc.Parameters.AddWithValue("@lt", d.LineTotal);
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

    public async Task<Quotation?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT q.*, a.AccountName
            FROM QuotationMaster q
            LEFT JOIN Accounts a ON q.AccountId = a.Id
            WHERE q.Id = @id AND q.TenantId = @tid AND q.IsDeleted = 0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapQuotation(reader);
        return null;
    }

    public async Task<List<Quotation>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT q.*, a.AccountName
            FROM QuotationMaster q
            LEFT JOIN Accounts a ON q.AccountId = a.Id
            WHERE q.TenantId = @tid AND q.IsDeleted = 0
              AND q.VoucherDate BETWEEN @from AND @to
            ORDER BY q.VoucherDate DESC";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));
        var list = new List<Quotation>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapQuotation(reader));
        return list;
    }

    public Task<string> GetNextVoucherNoAsync(string financialYear)
        => _settings.GetNextVoucherNoAsync("QT", financialYear);

    public async Task<bool> MarkConvertedAsync(int id, string saleVoucherNo)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE QuotationMaster SET IsConverted=1, ConvertedVchNo=@svno, UpdatedAt=@now
            WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@svno", saleVoucherNo);
        cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static Quotation MapQuotation(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
        VoucherNo = r.GetString(r.GetOrdinal("VoucherNo")),
        VoucherDate = DateTime.Parse(r.GetString(r.GetOrdinal("VoucherDate"))),
        AccountId = r.GetInt32(r.GetOrdinal("AccountId")),
        AccountName = r.IsDBNull(r.GetOrdinal("AccountName")) ? null : r.GetString(r.GetOrdinal("AccountName")),
        ValidDays = r.GetInt32(r.GetOrdinal("ValidDays")),
        NetAmount = r.GetDecimal(r.GetOrdinal("NetAmount")),
        Narration = r.IsDBNull(r.GetOrdinal("Narration")) ? null : r.GetString(r.GetOrdinal("Narration")),
        FinancialYear = r.GetString(r.GetOrdinal("FinancialYear")),
        IsConverted = r.GetInt32(r.GetOrdinal("IsConverted")) == 1,
        ConvertedVchNo = r.IsDBNull(r.GetOrdinal("ConvertedVchNo")) ? null : r.GetString(r.GetOrdinal("ConvertedVchNo")),
    };
}
