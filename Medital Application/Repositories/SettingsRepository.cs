using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly IDbConnectionFactory _db;
    public SettingsRepository(IDbConnectionFactory db) => _db = db;

    public async Task<string?> GetValueAsync(string key)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT SettingValue FROM AppSettings WHERE SettingKey=@key AND TenantId=@tid AND IsDeleted=0";
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        var result = await cmd.ExecuteScalarAsync();
        return result == DBNull.Value ? null : result?.ToString();
    }

    public async Task<bool> SetValueAsync(string key, string value, string category = "GENERAL")
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO AppSettings(TenantId,SettingKey,SettingValue,Category,UpdatedAt)
            VALUES(@tid,@key,@val,@cat,datetime('now'))
            ON CONFLICT(TenantId,SettingKey) DO UPDATE SET SettingValue=excluded.SettingValue,UpdatedAt=excluded.UpdatedAt";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@val", value);
        cmd.Parameters.AddWithValue("@cat", category);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<Dictionary<string, string>> GetByCategoryAsync(string category)
    {
        var dict = new Dictionary<string, string>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT SettingKey, SettingValue FROM AppSettings WHERE Category=@cat AND TenantId=@tid AND IsDeleted=0";
        cmd.Parameters.AddWithValue("@cat", category);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var k = r.GetString(0);
            var v = r.IsDBNull(1) ? "" : r.GetString(1);
            dict[k] = v;
        }
        return dict;
    }

    public async Task<Company?> GetCompanyProfileAsync()
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM CompanyProfile WHERE TenantId=@tid AND IsDeleted=0 LIMIT 1";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new Company
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
            CompanyName = r.GetString(r.GetOrdinal("CompanyName")),
            Address1 = SafeStr(r, "Address1"),
            Address2 = SafeStr(r, "Address2"),
            City = SafeStr(r, "City"),
            State = SafeStr(r, "State"),
            StateCode = SafeStr(r, "StateCode"),
            PinCode = SafeStr(r, "PinCode"),
            Phone = SafeStr(r, "Phone"),
            Mobile = SafeStr(r, "Mobile"),
            Email = SafeStr(r, "Email"),
            GSTIN = SafeStr(r, "GSTIN"),
            DrugLicense = SafeStr(r, "DrugLicense"),
            PAN = SafeStr(r, "PAN"),
            BankName = SafeStr(r, "BankName"),
            BankAccountNo = SafeStr(r, "BankAccountNo"),
            IFSCCode = SafeStr(r, "IFSCCode"),
            UPIId = SafeStr(r, "UPIId"),
            FinancialYear = r.GetString(r.GetOrdinal("FinancialYear")),
            YearStart = SafeStr(r, "YearStart"),
            YearEnd = SafeStr(r, "YearEnd"),
            LogoPath = SafeStr(r, "LogoPath"),
        };
    }

    public async Task<bool> SaveCompanyProfileAsync(Company company)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        if (company.Id == 0)
        {
            cmd.CommandText = @"INSERT INTO CompanyProfile
                (TenantId,CompanyName,Address1,Address2,City,State,StateCode,PinCode,Phone,Mobile,Email,
                 GSTIN,DrugLicense,PAN,BankName,BankAccountNo,IFSCCode,UPIId,FinancialYear,YearStart,YearEnd,LogoPath,
                 CreatedAt,UpdatedAt)
                VALUES(@tid,@nm,@a1,@a2,@city,@state,@stc,@pin,@ph,@mob,@email,
                 @gstin,@dl,@pan,@bank,@bankac,@ifsc,@upi,@fy,@ys,@ye,@logo,datetime('now'),datetime('now'))";
        }
        else
        {
            cmd.CommandText = @"UPDATE CompanyProfile SET
                CompanyName=@nm,Address1=@a1,Address2=@a2,City=@city,State=@state,StateCode=@stc,PinCode=@pin,
                Phone=@ph,Mobile=@mob,Email=@email,GSTIN=@gstin,DrugLicense=@dl,PAN=@pan,BankName=@bank,
                BankAccountNo=@bankac,IFSCCode=@ifsc,UPIId=@upi,FinancialYear=@fy,YearStart=@ys,YearEnd=@ye,
                LogoPath=@logo,UpdatedAt=datetime('now')
                WHERE TenantId=@tid";
        }
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@nm", company.CompanyName);
        cmd.Parameters.AddWithValue("@a1", company.Address1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@a2", company.Address2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@city", company.City ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@state", company.State ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@stc", company.StateCode ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@pin", company.PinCode ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ph", company.Phone ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@mob", company.Mobile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@email", company.Email ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@gstin", company.GSTIN ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@dl", company.DrugLicense ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@pan", company.PAN ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@bank", company.BankName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@bankac", company.BankAccountNo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ifsc", company.IFSCCode ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@upi", company.UPIId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@fy", company.FinancialYear);
        cmd.Parameters.AddWithValue("@ys", company.YearStart ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ye", company.YearEnd ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@logo", company.LogoPath ?? (object)DBNull.Value);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<VoucherSeries?> GetVoucherSeriesAsync(string voucherType, string financialYear)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT * FROM VoucherSeries
            WHERE TenantId=@tid AND VoucherType=@type AND FinancialYear=@fy AND IsDeleted=0";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@type", voucherType);
        cmd.Parameters.AddWithValue("@fy", financialYear);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new VoucherSeries
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
            VoucherType = r.GetString(r.GetOrdinal("VoucherType")),
            Prefix = r.GetString(r.GetOrdinal("Prefix")),
            FinancialYear = r.GetString(r.GetOrdinal("FinancialYear")),
            CurrentNo = r.GetInt32(r.GetOrdinal("CurrentNo")),
            Padding = r.GetInt32(r.GetOrdinal("Padding")),
        };
    }

    public async Task<string> GetNextVoucherNoAsync(string voucherType, string financialYear)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            using var selectCmd = conn.CreateCommand();
            selectCmd.Transaction = tx;
            selectCmd.CommandText = @"SELECT Id, Prefix, CurrentNo, Padding FROM VoucherSeries
                WHERE TenantId=@tid AND VoucherType=@type AND FinancialYear=@fy AND IsDeleted=0";
            selectCmd.Parameters.AddWithValue("@tid", _db.TenantId);
            selectCmd.Parameters.AddWithValue("@type", voucherType);
            selectCmd.Parameters.AddWithValue("@fy", financialYear);
            using var r = await selectCmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
            {
                await tx.RollbackAsync();
                return $"{voucherType}/{financialYear}/00001";
            }
            var id = r.GetInt32(0);
            var prefix = r.GetString(1);
            var current = r.GetInt32(2);
            var padding = r.GetInt32(3);
            r.Close();

            var next = current + 1;
            using var updateCmd = conn.CreateCommand();
            updateCmd.Transaction = tx;
            updateCmd.CommandText = "UPDATE VoucherSeries SET CurrentNo=@next,UpdatedAt=datetime('now') WHERE Id=@id";
            updateCmd.Parameters.AddWithValue("@next", next);
            updateCmd.Parameters.AddWithValue("@id", id);
            await updateCmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
            return $"{prefix}/{financialYear}/{next.ToString().PadLeft(padding, '0')}";
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static string? SafeStr(SqliteDataReader r, string col)
    {
        try { var i = r.GetOrdinal(col); return r.IsDBNull(i) ? null : r.GetString(i); }
        catch { return null; }
    }
}
