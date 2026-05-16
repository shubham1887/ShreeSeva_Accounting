using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class DebitNoteRepository : IDebitNoteRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ISettingsRepository _settings;

    public DebitNoteRepository(IDbConnectionFactory db, ISettingsRepository settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<int> CreateAsync(DebitNoteMaster master, List<DebitNoteDetail> details)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO DebitNoteMaster
                    (TenantId, VoucherNo, VoucherDate, AccountId, RefVoucherNo,
                     GrossAmount, TotalSGST, TotalCGST, TotalIGST, NetAmount,
                     Narration, FinancialYear, IsDeleted, CreatedAt, UpdatedAt)
                VALUES
                    (@tid, @vno, @dt, @aid, @ref,
                     @gross, @sgst, @cgst, @igst, @net,
                     @narr, @fy, 0, @now, @now);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@tid", _db.TenantId);
            cmd.Parameters.AddWithValue("@vno", master.VoucherNo);
            cmd.Parameters.AddWithValue("@dt", master.VoucherDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@aid", master.AccountId);
            cmd.Parameters.AddWithValue("@ref", (object?)master.RefVoucherNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@gross", master.GrossAmount);
            cmd.Parameters.AddWithValue("@sgst", master.TotalSGST);
            cmd.Parameters.AddWithValue("@cgst", master.TotalCGST);
            cmd.Parameters.AddWithValue("@igst", master.TotalIGST);
            cmd.Parameters.AddWithValue("@net", master.NetAmount);
            cmd.Parameters.AddWithValue("@narr", (object?)master.Narration ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fy", master.FinancialYear);
            cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            var masterId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            foreach (var d in details)
            {
                var dc = conn.CreateCommand();
                dc.Transaction = tx;
                dc.CommandText = @"
                    INSERT INTO DebitNoteDetails
                        (TenantId, DebitNoteMasterId, ProductId, BatchNo, ExpiryMY, ReturnQty,
                         PurchaseRate, MRP, SGSTRate, CGSTRate, IGSTRate, SGSTAmount, CGSTAmount, IGSTAmount,
                         TaxableAmount, LineTotal, IsDeleted, CreatedAt)
                    VALUES
                        (@tid, @dnid, @pid, @batch, @exp, @qty,
                         @rate, @mrp, @sgst, @cgst, @igst, @sgsta, @cgsta, @igsta,
                         @taxable, @lt, 0, @now)";
                dc.Parameters.AddWithValue("@tid", _db.TenantId);
                dc.Parameters.AddWithValue("@dnid", masterId);
                dc.Parameters.AddWithValue("@pid", d.ProductId);
                dc.Parameters.AddWithValue("@batch", d.BatchNo);
                dc.Parameters.AddWithValue("@exp", d.ExpiryMY);
                dc.Parameters.AddWithValue("@qty", d.ReturnQty);
                dc.Parameters.AddWithValue("@rate", d.PurchaseRate);
                dc.Parameters.AddWithValue("@mrp", d.MRP);
                dc.Parameters.AddWithValue("@sgst", d.SGSTRate);
                dc.Parameters.AddWithValue("@cgst", d.CGSTRate);
                dc.Parameters.AddWithValue("@igst", d.IGSTRate);
                dc.Parameters.AddWithValue("@sgsta", d.SGSTAmount);
                dc.Parameters.AddWithValue("@cgsta", d.CGSTAmount);
                dc.Parameters.AddWithValue("@igsta", d.IGSTAmount);
                dc.Parameters.AddWithValue("@taxable", d.TaxableAmount);
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

    public async Task<DebitNoteMaster?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT dn.*, a.AccountName
            FROM DebitNoteMaster dn
            LEFT JOIN Accounts a ON dn.AccountId = a.Id
            WHERE dn.Id = @id AND dn.TenantId = @tid AND dn.IsDeleted = 0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapMaster(reader);
        return null;
    }

    public async Task<List<DebitNoteMaster>> GetByDateRangeAsync(DateTime from, DateTime to, int? accountId = null)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT dn.*, a.AccountName
            FROM DebitNoteMaster dn
            LEFT JOIN Accounts a ON dn.AccountId = a.Id
            WHERE dn.TenantId = @tid AND dn.IsDeleted = 0
              AND dn.VoucherDate BETWEEN @from AND @to";
        if (accountId.HasValue)
        {
            cmd.CommandText += " AND dn.AccountId = @aid";
            cmd.Parameters.AddWithValue("@aid", accountId.Value);
        }
        cmd.CommandText += " ORDER BY dn.VoucherDate DESC, dn.Id DESC";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));
        var list = new List<DebitNoteMaster>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapMaster(reader));
        return list;
    }

    public async Task<List<DebitNoteDetail>> GetDetailsAsync(int masterId)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT d.*, p.ProductName
            FROM DebitNoteDetails d
            LEFT JOIN Products p ON d.ProductId = p.Id
            WHERE d.DebitNoteMasterId = @mid AND d.TenantId = @tid AND d.IsDeleted = 0";
        cmd.Parameters.AddWithValue("@mid", masterId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        var list = new List<DebitNoteDetail>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new DebitNoteDetail
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                DebitNoteMasterId = reader.GetInt32(reader.GetOrdinal("DebitNoteMasterId")),
                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? null : reader.GetString(reader.GetOrdinal("ProductName")),
                BatchNo = reader.GetString(reader.GetOrdinal("BatchNo")),
                ExpiryMY = reader.IsDBNull(reader.GetOrdinal("ExpiryMY")) ? "" : reader.GetString(reader.GetOrdinal("ExpiryMY")),
                ReturnQty = reader.GetDecimal(reader.GetOrdinal("ReturnQty")),
                PurchaseRate = reader.GetDecimal(reader.GetOrdinal("PurchaseRate")),
                MRP = reader.GetDecimal(reader.GetOrdinal("MRP")),
                SGSTRate = reader.GetDecimal(reader.GetOrdinal("SGSTRate")),
                CGSTRate = reader.GetDecimal(reader.GetOrdinal("CGSTRate")),
                IGSTRate = reader.GetDecimal(reader.GetOrdinal("IGSTRate")),
                SGSTAmount = reader.GetDecimal(reader.GetOrdinal("SGSTAmount")),
                CGSTAmount = reader.GetDecimal(reader.GetOrdinal("CGSTAmount")),
                IGSTAmount = reader.GetDecimal(reader.GetOrdinal("IGSTAmount")),
                TaxableAmount = reader.GetDecimal(reader.GetOrdinal("TaxableAmount")),
                LineTotal = reader.GetDecimal(reader.GetOrdinal("LineTotal")),
            });
        return list;
    }

    public Task<string> GetNextVoucherNoAsync(string financialYear)
        => _settings.GetNextVoucherNoAsync("DN", financialYear);

    private static DebitNoteMaster MapMaster(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
        VoucherNo = r.GetString(r.GetOrdinal("VoucherNo")),
        VoucherDate = DateTime.Parse(r.GetString(r.GetOrdinal("VoucherDate"))),
        AccountId = r.GetInt32(r.GetOrdinal("AccountId")),
        AccountName = r.IsDBNull(r.GetOrdinal("AccountName")) ? null : r.GetString(r.GetOrdinal("AccountName")),
        RefVoucherNo = r.IsDBNull(r.GetOrdinal("RefVoucherNo")) ? null : r.GetString(r.GetOrdinal("RefVoucherNo")),
        GrossAmount = r.GetDecimal(r.GetOrdinal("GrossAmount")),
        TotalSGST = r.GetDecimal(r.GetOrdinal("TotalSGST")),
        TotalCGST = r.GetDecimal(r.GetOrdinal("TotalCGST")),
        TotalIGST = r.GetDecimal(r.GetOrdinal("TotalIGST")),
        NetAmount = r.GetDecimal(r.GetOrdinal("NetAmount")),
        Narration = r.IsDBNull(r.GetOrdinal("Narration")) ? null : r.GetString(r.GetOrdinal("Narration")),
        FinancialYear = r.GetString(r.GetOrdinal("FinancialYear")),
    };
}
