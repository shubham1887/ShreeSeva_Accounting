using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ISettingsRepository _settings;

    public PurchaseRepository(IDbConnectionFactory db, ISettingsRepository settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<int> CreateAsync(PurchaseMaster master, List<PurchaseDetail> details)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            using var mCmd = conn.CreateCommand();
            mCmd.Transaction = tx;
            mCmd.CommandText = @"INSERT INTO PurchaseMaster
                (TenantId,VoucherNo,VoucherDate,BillNo,BillDate,ChallanNo,ChallanDate,AccountId,
                 GrossAmount,ItemDiscAmount,SpecialDisc,FreightAmount,TotalSGST,TotalCGST,TotalIGST,
                 RoundOff,NetAmount,FinancialYear,Narration,CreatedAt,UpdatedAt)
                VALUES(@tid,@vno,@vdt,@bno,@bdt,@cno,@cdt,@acid,
                 @gross,@idisc,@sdisc,@freight,@sgst,@cgst,@igst,
                 @round,@net,@fy,@nara,datetime('now'),datetime('now'));
                SELECT last_insert_rowid();";
            mCmd.Parameters.AddWithValue("@tid", _db.TenantId);
            mCmd.Parameters.AddWithValue("@vno", master.VoucherNo);
            mCmd.Parameters.AddWithValue("@vdt", master.VoucherDate.ToString("yyyy-MM-dd"));
            mCmd.Parameters.AddWithValue("@bno", master.BillNo ?? (object)DBNull.Value);
            mCmd.Parameters.AddWithValue("@bdt", master.BillDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            mCmd.Parameters.AddWithValue("@cno", master.ChallanNo ?? (object)DBNull.Value);
            mCmd.Parameters.AddWithValue("@cdt", master.ChallanDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            mCmd.Parameters.AddWithValue("@acid", master.AccountId);
            mCmd.Parameters.AddWithValue("@gross", master.GrossAmount);
            mCmd.Parameters.AddWithValue("@idisc", master.ItemDiscAmount);
            mCmd.Parameters.AddWithValue("@sdisc", master.SpecialDisc);
            mCmd.Parameters.AddWithValue("@freight", master.FreightAmount);
            mCmd.Parameters.AddWithValue("@sgst", master.TotalSGST);
            mCmd.Parameters.AddWithValue("@cgst", master.TotalCGST);
            mCmd.Parameters.AddWithValue("@igst", master.TotalIGST);
            mCmd.Parameters.AddWithValue("@round", master.RoundOff);
            mCmd.Parameters.AddWithValue("@net", master.NetAmount);
            mCmd.Parameters.AddWithValue("@fy", master.FinancialYear);
            mCmd.Parameters.AddWithValue("@nara", master.Narration ?? (object)DBNull.Value);
            var masterId = Convert.ToInt32(await mCmd.ExecuteScalarAsync());

            foreach (var d in details)
            {
                using var dCmd = conn.CreateCommand();
                dCmd.Transaction = tx;
                dCmd.CommandText = @"INSERT INTO PurchaseDetails
                    (TenantId,PurchaseMasterId,ProductId,BatchNo,ExpiryMY,ExpiryDate,Quantity,FreeQuantity,SchemeQty,
                     ActualRate,NetRate,MRP,SaleRate,ItemDiscPer,ItemDiscAmt,SGSTRate,CGSTRate,IGSTRate,
                     SGSTAmount,CGSTAmount,IGSTAmount,TaxableAmount,LineTotal,StockKey,CreatedAt,UpdatedAt)
                    VALUES(@tid,@mid,@pid,@batch,@expmy,@expdt,@qty,@freeqty,@schqty,
                     @ar,@nr,@mrp,@sr,@idper,@idamt,@sgstp,@cgstp,@igstp,
                     @sgsta,@cgsta,@igsta,@tax,@line,@skey,datetime('now'),datetime('now'))";
                dCmd.Parameters.AddWithValue("@tid", _db.TenantId);
                dCmd.Parameters.AddWithValue("@mid", masterId);
                dCmd.Parameters.AddWithValue("@pid", d.ProductId);
                dCmd.Parameters.AddWithValue("@batch", d.BatchNo);
                dCmd.Parameters.AddWithValue("@expmy", d.ExpiryMY);
                dCmd.Parameters.AddWithValue("@expdt", d.ExpiryDate);
                dCmd.Parameters.AddWithValue("@qty", d.Quantity);
                dCmd.Parameters.AddWithValue("@freeqty", d.FreeQuantity);
                dCmd.Parameters.AddWithValue("@schqty", d.SchemeQty);
                dCmd.Parameters.AddWithValue("@ar", d.ActualRate);
                dCmd.Parameters.AddWithValue("@nr", d.NetRate);
                dCmd.Parameters.AddWithValue("@mrp", d.MRP);
                dCmd.Parameters.AddWithValue("@sr", d.SaleRate);
                dCmd.Parameters.AddWithValue("@idper", d.ItemDiscPer);
                dCmd.Parameters.AddWithValue("@idamt", d.ItemDiscAmt);
                dCmd.Parameters.AddWithValue("@sgstp", d.SGSTRate);
                dCmd.Parameters.AddWithValue("@cgstp", d.CGSTRate);
                dCmd.Parameters.AddWithValue("@igstp", d.IGSTRate);
                dCmd.Parameters.AddWithValue("@sgsta", d.SGSTAmount);
                dCmd.Parameters.AddWithValue("@cgsta", d.CGSTAmount);
                dCmd.Parameters.AddWithValue("@igsta", d.IGSTAmount);
                dCmd.Parameters.AddWithValue("@tax", d.TaxableAmount);
                dCmd.Parameters.AddWithValue("@line", d.LineTotal);
                dCmd.Parameters.AddWithValue("@skey", d.StockKey);
                await dCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return masterId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<PurchaseMaster?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT pm.*, a.AccountName FROM PurchaseMaster pm
            JOIN Accounts a ON a.Id=pm.AccountId
            WHERE pm.Id=@id AND pm.TenantId=@tid AND pm.IsDeleted=0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        var master = MapMaster(r);
        master.Details = await GetDetailsAsync(master.Id);
        return master;
    }

    public async Task<PurchaseMaster?> GetByVoucherNoAsync(string voucherNo)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT pm.*, a.AccountName FROM PurchaseMaster pm
            JOIN Accounts a ON a.Id=pm.AccountId
            WHERE pm.VoucherNo=@vno AND pm.TenantId=@tid AND pm.IsDeleted=0";
        cmd.Parameters.AddWithValue("@vno", voucherNo);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapMaster(r) : null;
    }

    public async Task<List<PurchaseMaster>> GetByDateRangeAsync(DateTime from, DateTime to, int? accountId = null)
    {
        var list = new List<PurchaseMaster>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        var where = "pm.TenantId=@tid AND pm.IsDeleted=0 AND pm.VoucherDate BETWEEN @from AND @to";
        if (accountId.HasValue) where += " AND pm.AccountId=@aid";
        cmd.CommandText = $@"SELECT pm.*, a.AccountName FROM PurchaseMaster pm
            JOIN Accounts a ON a.Id=pm.AccountId
            WHERE {where} ORDER BY pm.VoucherDate DESC, pm.Id DESC";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@aid", accountId ?? (object)DBNull.Value);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapMaster(r));
        return list;
    }

    public async Task<List<PurchaseDetail>> GetDetailsAsync(int purchaseMasterId)
    {
        var list = new List<PurchaseDetail>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT pd.*, p.ProductName, p.HSNCode FROM PurchaseDetails pd
            JOIN Products p ON p.Id=pd.ProductId
            WHERE pd.PurchaseMasterId=@mid AND pd.TenantId=@tid AND pd.IsDeleted=0";
        cmd.Parameters.AddWithValue("@mid", purchaseMasterId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new PurchaseDetail
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
                PurchaseMasterId = r.GetInt32(r.GetOrdinal("PurchaseMasterId")),
                ProductId = r.GetInt32(r.GetOrdinal("ProductId")),
                BatchNo = r.GetString(r.GetOrdinal("BatchNo")),
                ExpiryMY = r.GetString(r.GetOrdinal("ExpiryMY")),
                ExpiryDate = r.GetString(r.GetOrdinal("ExpiryDate")),
                Quantity = r.GetDecimal(r.GetOrdinal("Quantity")),
                FreeQuantity = r.GetDecimal(r.GetOrdinal("FreeQuantity")),
                SchemeQty = r.GetDecimal(r.GetOrdinal("SchemeQty")),
                ActualRate = r.GetDecimal(r.GetOrdinal("ActualRate")),
                NetRate = r.GetDecimal(r.GetOrdinal("NetRate")),
                MRP = r.GetDecimal(r.GetOrdinal("MRP")),
                SaleRate = r.GetDecimal(r.GetOrdinal("SaleRate")),
                ItemDiscPer = r.GetDecimal(r.GetOrdinal("ItemDiscPer")),
                ItemDiscAmt = r.GetDecimal(r.GetOrdinal("ItemDiscAmt")),
                SGSTRate = r.GetDecimal(r.GetOrdinal("SGSTRate")),
                CGSTRate = r.GetDecimal(r.GetOrdinal("CGSTRate")),
                IGSTRate = r.GetDecimal(r.GetOrdinal("IGSTRate")),
                SGSTAmount = r.GetDecimal(r.GetOrdinal("SGSTAmount")),
                CGSTAmount = r.GetDecimal(r.GetOrdinal("CGSTAmount")),
                IGSTAmount = r.GetDecimal(r.GetOrdinal("IGSTAmount")),
                TaxableAmount = r.GetDecimal(r.GetOrdinal("TaxableAmount")),
                LineTotal = r.GetDecimal(r.GetOrdinal("LineTotal")),
                StockKey = r.GetString(r.GetOrdinal("StockKey")),
                ProductName = SafeStr(r, "ProductName"),
                HSNCode = SafeStr(r, "HSNCode"),
            });
        }
        return list;
    }

    public Task<string> GetNextVoucherNoAsync(string financialYear) =>
        _settings.GetNextVoucherNoAsync("PURCHASE", financialYear);

    public async Task<decimal> GetTotalByDateAsync(DateTime date)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT COALESCE(SUM(NetAmount),0) FROM PurchaseMaster
            WHERE TenantId=@tid AND VoucherDate=@dt AND IsCancelled=0 AND IsDeleted=0";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@dt", date.ToString("yyyy-MM-dd"));
        return Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> CancelAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE PurchaseMaster SET IsCancelled=1,UpdatedAt=datetime('now') WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<PurchaseMaster>> GetUnpaidAsync(int accountId)
    {
        var list = new List<PurchaseMaster>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT pm.*, a.AccountName,
            pm.NetAmount - COALESCE((SELECT SUM(pd2.AllocatedAmount) FROM PaymentDetails pd2 WHERE pd2.PurchaseMasterId=pm.Id AND pd2.IsDeleted=0),0) AS Outstanding
            FROM PurchaseMaster pm
            JOIN Accounts a ON a.Id=pm.AccountId
            WHERE pm.AccountId=@aid AND pm.TenantId=@tid AND pm.IsDeleted=0 AND pm.IsCancelled=0
            HAVING Outstanding > 0
            ORDER BY pm.VoucherDate";
        cmd.Parameters.AddWithValue("@aid", accountId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapMaster(r));
        return list;
    }

    private static PurchaseMaster MapMaster(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
        VoucherNo = r.GetString(r.GetOrdinal("VoucherNo")),
        VoucherDate = DateTime.Parse(r.GetString(r.GetOrdinal("VoucherDate"))),
        BillNo = SafeStr(r, "BillNo"),
        BillDate = SafeStr(r, "BillDate") is string bd ? DateTime.Parse(bd) : null,
        AccountId = r.GetInt32(r.GetOrdinal("AccountId")),
        GrossAmount = r.GetDecimal(r.GetOrdinal("GrossAmount")),
        ItemDiscAmount = r.GetDecimal(r.GetOrdinal("ItemDiscAmount")),
        SpecialDisc = r.GetDecimal(r.GetOrdinal("SpecialDisc")),
        FreightAmount = r.GetDecimal(r.GetOrdinal("FreightAmount")),
        TotalSGST = r.GetDecimal(r.GetOrdinal("TotalSGST")),
        TotalCGST = r.GetDecimal(r.GetOrdinal("TotalCGST")),
        TotalIGST = r.GetDecimal(r.GetOrdinal("TotalIGST")),
        RoundOff = r.GetDecimal(r.GetOrdinal("RoundOff")),
        NetAmount = r.GetDecimal(r.GetOrdinal("NetAmount")),
        FinancialYear = r.GetString(r.GetOrdinal("FinancialYear")),
        IsCancelled = r.GetInt32(r.GetOrdinal("IsCancelled")) == 1,
        IsDeleted = r.GetInt32(r.GetOrdinal("IsDeleted")) == 1,
        AccountName = SafeStr(r, "AccountName"),
    };

    private static string? SafeStr(SqliteDataReader r, string col)
    {
        try { var i = r.GetOrdinal(col); return r.IsDBNull(i) ? null : r.GetString(i); }
        catch { return null; }
    }
}
