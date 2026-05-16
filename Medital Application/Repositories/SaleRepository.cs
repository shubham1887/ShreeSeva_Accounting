using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ISettingsRepository _settings;

    public SaleRepository(IDbConnectionFactory db, ISettingsRepository settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<int> CreateAsync(SaleMaster master, List<SaleDetail> details)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            // Insert master
            using var mCmd = conn.CreateCommand();
            mCmd.Transaction = tx;
            mCmd.CommandText = @"INSERT INTO SaleMaster
                (TenantId,VoucherNo,VoucherDate,TransactionType,AccountId,PatientId,DoctorId,
                 GrossAmount,ItemDiscAmount,CashDiscPer,CashDiscAmount,TotalSGST,TotalCGST,TotalIGST,
                 RoundOff,NetAmount,PaymentMode,ChequeNo,ChequeDate,UPIRef,Narration,FinancialYear,
                 IsInterState,CreatedAt,UpdatedAt)
                VALUES(@tid,@vno,@vdt,@ttype,@acid,@patid,@docid,
                 @gross,@idisc,@cdper,@cdamt,@sgst,@cgst,@igst,
                 @round,@net,@paymode,@chqno,@chqdt,@upiref,@nara,@fy,
                 @istate,datetime('now'),datetime('now'));
                SELECT last_insert_rowid();";
            mCmd.Parameters.AddWithValue("@tid", _db.TenantId);
            mCmd.Parameters.AddWithValue("@vno", master.VoucherNo);
            mCmd.Parameters.AddWithValue("@vdt", master.VoucherDate.ToString("yyyy-MM-dd"));
            mCmd.Parameters.AddWithValue("@ttype", master.TransactionType);
            mCmd.Parameters.AddWithValue("@acid", master.AccountId);
            mCmd.Parameters.AddWithValue("@patid", master.PatientId ?? (object)DBNull.Value);
            mCmd.Parameters.AddWithValue("@docid", master.DoctorId ?? (object)DBNull.Value);
            mCmd.Parameters.AddWithValue("@gross", master.GrossAmount);
            mCmd.Parameters.AddWithValue("@idisc", master.ItemDiscAmount);
            mCmd.Parameters.AddWithValue("@cdper", master.CashDiscPer);
            mCmd.Parameters.AddWithValue("@cdamt", master.CashDiscAmount);
            mCmd.Parameters.AddWithValue("@sgst", master.TotalSGST);
            mCmd.Parameters.AddWithValue("@cgst", master.TotalCGST);
            mCmd.Parameters.AddWithValue("@igst", master.TotalIGST);
            mCmd.Parameters.AddWithValue("@round", master.RoundOff);
            mCmd.Parameters.AddWithValue("@net", master.NetAmount);
            mCmd.Parameters.AddWithValue("@paymode", master.PaymentMode);
            mCmd.Parameters.AddWithValue("@chqno", master.ChequeNo ?? (object)DBNull.Value);
            mCmd.Parameters.AddWithValue("@chqdt", master.ChequeDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            mCmd.Parameters.AddWithValue("@upiref", master.UPIRef ?? (object)DBNull.Value);
            mCmd.Parameters.AddWithValue("@nara", master.Narration ?? (object)DBNull.Value);
            mCmd.Parameters.AddWithValue("@fy", master.FinancialYear);
            mCmd.Parameters.AddWithValue("@istate", master.IsInterState ? 1 : 0);
            var masterId = Convert.ToInt32(await mCmd.ExecuteScalarAsync());

            // Insert details
            foreach (var d in details)
            {
                using var dCmd = conn.CreateCommand();
                dCmd.Transaction = tx;
                dCmd.CommandText = @"INSERT INTO SaleDetails
                    (TenantId,SaleMasterId,ProductId,BatchNo,ExpiryMY,ExpiryDate,Quantity,FreeQuantity,
                     SaleRate,MRP,ItemDiscPer,ItemDiscAmt,SGSTRate,CGSTRate,IGSTRate,
                     SGSTAmount,CGSTAmount,IGSTAmount,TaxableAmount,LineTotal,PurchaseRate,Profit,StockKey,StockId,
                     CreatedAt,UpdatedAt)
                    VALUES(@tid,@mid,@pid,@batch,@expmy,@expdt,@qty,@freeqty,
                     @sr,@mrp,@idper,@idamt,@sgstp,@cgstp,@igstp,
                     @sgsta,@cgsta,@igsta,@tax,@line,@purrate,@profit,@skey,@stid,
                     datetime('now'),datetime('now'))";
                dCmd.Parameters.AddWithValue("@tid", _db.TenantId);
                dCmd.Parameters.AddWithValue("@mid", masterId);
                dCmd.Parameters.AddWithValue("@pid", d.ProductId);
                dCmd.Parameters.AddWithValue("@batch", d.BatchNo);
                dCmd.Parameters.AddWithValue("@expmy", d.ExpiryMY);
                dCmd.Parameters.AddWithValue("@expdt", d.ExpiryDate);
                dCmd.Parameters.AddWithValue("@qty", d.Quantity);
                dCmd.Parameters.AddWithValue("@freeqty", d.FreeQuantity);
                dCmd.Parameters.AddWithValue("@sr", d.SaleRate);
                dCmd.Parameters.AddWithValue("@mrp", d.MRP);
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
                dCmd.Parameters.AddWithValue("@purrate", d.PurchaseRate);
                dCmd.Parameters.AddWithValue("@profit", d.Profit);
                dCmd.Parameters.AddWithValue("@skey", d.StockKey);
                dCmd.Parameters.AddWithValue("@stid", d.StockId ?? (object)DBNull.Value);
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

    public async Task<SaleMaster?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT s.*, a.AccountName, p.PatientName, d.DoctorName
            FROM SaleMaster s
            JOIN Accounts a ON a.Id=s.AccountId
            LEFT JOIN Patients p ON p.Id=s.PatientId
            LEFT JOIN Doctors d ON d.Id=s.DoctorId
            WHERE s.Id=@id AND s.TenantId=@tid AND s.IsDeleted=0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        var master = MapMaster(r);
        master.Details = await GetDetailsAsync(master.Id);
        return master;
    }

    public async Task<SaleMaster?> GetByVoucherNoAsync(string voucherNo)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT s.*, a.AccountName, p.PatientName, d.DoctorName
            FROM SaleMaster s
            JOIN Accounts a ON a.Id=s.AccountId
            LEFT JOIN Patients p ON p.Id=s.PatientId
            LEFT JOIN Doctors d ON d.Id=s.DoctorId
            WHERE s.VoucherNo=@vno AND s.TenantId=@tid AND s.IsDeleted=0";
        cmd.Parameters.AddWithValue("@vno", voucherNo);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return MapMaster(r);
    }

    public async Task<List<SaleMaster>> GetByDateRangeAsync(DateTime from, DateTime to, int? accountId = null)
    {
        var list = new List<SaleMaster>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        var where = "s.TenantId=@tid AND s.IsDeleted=0 AND s.VoucherDate BETWEEN @from AND @to";
        if (accountId.HasValue) where += " AND s.AccountId=@aid";
        cmd.CommandText = $@"SELECT s.*, a.AccountName, p.PatientName, d.DoctorName
            FROM SaleMaster s
            JOIN Accounts a ON a.Id=s.AccountId
            LEFT JOIN Patients p ON p.Id=s.PatientId
            LEFT JOIN Doctors d ON d.Id=s.DoctorId
            WHERE {where} ORDER BY s.VoucherDate DESC, s.Id DESC";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@aid", accountId ?? (object)DBNull.Value);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapMaster(r));
        return list;
    }

    public async Task<List<SaleDetail>> GetDetailsAsync(int saleMasterId)
    {
        var list = new List<SaleDetail>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT sd.*, p.ProductName, p.HSNCode
            FROM SaleDetails sd JOIN Products p ON p.Id=sd.ProductId
            WHERE sd.SaleMasterId=@mid AND sd.TenantId=@tid AND sd.IsDeleted=0";
        cmd.Parameters.AddWithValue("@mid", saleMasterId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new SaleDetail
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
                SaleMasterId = r.GetInt32(r.GetOrdinal("SaleMasterId")),
                ProductId = r.GetInt32(r.GetOrdinal("ProductId")),
                BatchNo = r.GetString(r.GetOrdinal("BatchNo")),
                ExpiryMY = r.GetString(r.GetOrdinal("ExpiryMY")),
                ExpiryDate = r.GetString(r.GetOrdinal("ExpiryDate")),
                Quantity = r.GetDecimal(r.GetOrdinal("Quantity")),
                FreeQuantity = r.GetDecimal(r.GetOrdinal("FreeQuantity")),
                SaleRate = r.GetDecimal(r.GetOrdinal("SaleRate")),
                MRP = r.GetDecimal(r.GetOrdinal("MRP")),
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
                PurchaseRate = r.GetDecimal(r.GetOrdinal("PurchaseRate")),
                Profit = r.GetDecimal(r.GetOrdinal("Profit")),
                StockKey = r.GetString(r.GetOrdinal("StockKey")),
                StockId = r.IsDBNull(r.GetOrdinal("StockId")) ? null : r.GetInt32(r.GetOrdinal("StockId")),
                ProductName = r.IsDBNull(r.GetOrdinal("ProductName")) ? null : r.GetString(r.GetOrdinal("ProductName")),
                HSNCode = r.IsDBNull(r.GetOrdinal("HSNCode")) ? null : r.GetString(r.GetOrdinal("HSNCode")),
            });
        }
        return list;
    }

    public Task<string> GetNextVoucherNoAsync(string financialYear) =>
        _settings.GetNextVoucherNoAsync("SALE", financialYear);

    public async Task<decimal> GetTotalByDateAsync(DateTime date)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT COALESCE(SUM(NetAmount),0) FROM SaleMaster
            WHERE TenantId=@tid AND VoucherDate=@dt AND IsCancelled=0 AND IsDeleted=0 AND TransactionType='SA'";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@dt", date.ToString("yyyy-MM-dd"));
        return Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
    }

    public async Task<decimal> GetTotalByMonthAsync(int year, int month)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        var from = new DateTime(year, month, 1).ToString("yyyy-MM-dd");
        var to = new DateTime(year, month, DateTime.DaysInMonth(year, month)).ToString("yyyy-MM-dd");
        cmd.CommandText = @"SELECT COALESCE(SUM(NetAmount),0) FROM SaleMaster
            WHERE TenantId=@tid AND VoucherDate BETWEEN @from AND @to
            AND IsCancelled=0 AND IsDeleted=0 AND TransactionType='SA'";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@to", to);
        return Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> CancelAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE SaleMaster SET IsCancelled=1,UpdatedAt=datetime('now') WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static SaleMaster MapMaster(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
        VoucherNo = r.GetString(r.GetOrdinal("VoucherNo")),
        VoucherDate = DateTime.Parse(r.GetString(r.GetOrdinal("VoucherDate"))),
        TransactionType = r.GetString(r.GetOrdinal("TransactionType")),
        AccountId = r.GetInt32(r.GetOrdinal("AccountId")),
        PatientId = r.IsDBNull(r.GetOrdinal("PatientId")) ? null : r.GetInt32(r.GetOrdinal("PatientId")),
        DoctorId = r.IsDBNull(r.GetOrdinal("DoctorId")) ? null : r.GetInt32(r.GetOrdinal("DoctorId")),
        GrossAmount = r.GetDecimal(r.GetOrdinal("GrossAmount")),
        ItemDiscAmount = r.GetDecimal(r.GetOrdinal("ItemDiscAmount")),
        CashDiscPer = r.GetDecimal(r.GetOrdinal("CashDiscPer")),
        CashDiscAmount = r.GetDecimal(r.GetOrdinal("CashDiscAmount")),
        TotalSGST = r.GetDecimal(r.GetOrdinal("TotalSGST")),
        TotalCGST = r.GetDecimal(r.GetOrdinal("TotalCGST")),
        TotalIGST = r.GetDecimal(r.GetOrdinal("TotalIGST")),
        RoundOff = r.GetDecimal(r.GetOrdinal("RoundOff")),
        NetAmount = r.GetDecimal(r.GetOrdinal("NetAmount")),
        PaymentMode = r.GetString(r.GetOrdinal("PaymentMode")),
        FinancialYear = r.GetString(r.GetOrdinal("FinancialYear")),
        IsInterState = r.GetInt32(r.GetOrdinal("IsInterState")) == 1,
        IsCancelled = r.GetInt32(r.GetOrdinal("IsCancelled")) == 1,
        IsDeleted = r.GetInt32(r.GetOrdinal("IsDeleted")) == 1,
        AccountName = SafeStr(r, "AccountName"),
        PatientName = SafeStr(r, "PatientName"),
        DoctorName = SafeStr(r, "DoctorName"),
    };

    private static string? SafeStr(SqliteDataReader r, string col)
    {
        try { var i = r.GetOrdinal(col); return r.IsDBNull(i) ? null : r.GetString(i); }
        catch { return null; }
    }
}
