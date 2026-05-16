using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly IDbConnectionFactory _db;
    public AccountRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<Account>> SearchAsync(string? searchTerm, int? groupId = null)
    {
        var list = new List<Account>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        var where = "a.TenantId=@tid AND a.IsDeleted=0";
        if (!string.IsNullOrWhiteSpace(searchTerm))
            where += " AND (a.AccountName LIKE @s OR a.Mobile LIKE @s OR a.GSTIN LIKE @s)";
        if (groupId.HasValue)
            where += " AND a.GroupId=@grp";

        cmd.CommandText = $@"
            SELECT a.*, ag.GroupName, ag.GroupCode
            FROM Accounts a
            LEFT JOIN AccountGroups ag ON ag.Id=a.GroupId
            WHERE {where} ORDER BY a.AccountName LIMIT 100";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@s", $"%{searchTerm}%");
        cmd.Parameters.AddWithValue("@grp", groupId ?? (object)DBNull.Value);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapAccount(r));
        return list;
    }

    public async Task<Account?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT a.*, ag.GroupName, ag.GroupCode FROM Accounts a
            LEFT JOIN AccountGroups ag ON ag.Id=a.GroupId
            WHERE a.Id=@id AND a.TenantId=@tid AND a.IsDeleted=0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapAccount(r) : null;
    }

    public async Task<int> CreateAsync(Account acc)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Accounts
            (TenantId,AccountCode,AccountName,Address1,Address2,Address3,Address4,Area,City,State,StateCode,PinCode,
             Phone,Mobile,Email,GSTIN,GroupId,CashDiscountPer,BankName,BankAccountNo,IFSCCode,DrugLicenseNo,PANNo,
             OpeningBalance,OpeningDr,DueDays,IsLocked,IsInactive,CreatedAt,UpdatedAt)
            VALUES(@tid,@code,@name,@a1,@a2,@a3,@a4,@area,@city,@state,@stcode,@pin,
             @phone,@mob,@email,@gstin,@grp,@cd,@bank,@bankac,@ifsc,@dl,@pan,
             @opbal,@opdr,@due,@locked,@inactive,datetime('now'),datetime('now'));
            SELECT last_insert_rowid();";
        AddAccountParams(cmd, acc);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<bool> UpdateAsync(Account acc)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Accounts SET
            AccountCode=@code,AccountName=@name,Address1=@a1,Address2=@a2,Address3=@a3,Address4=@a4,
            Area=@area,City=@city,State=@state,StateCode=@stcode,PinCode=@pin,Phone=@phone,Mobile=@mob,
            Email=@email,GSTIN=@gstin,GroupId=@grp,CashDiscountPer=@cd,BankName=@bank,BankAccountNo=@bankac,
            IFSCCode=@ifsc,DrugLicenseNo=@dl,PANNo=@pan,OpeningBalance=@opbal,OpeningDr=@opdr,
            DueDays=@due,IsLocked=@locked,IsInactive=@inactive,UpdatedAt=datetime('now')
            WHERE Id=@id AND TenantId=@tid";
        AddAccountParams(cmd, acc);
        cmd.Parameters.AddWithValue("@id", acc.Id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Accounts SET IsDeleted=1,UpdatedAt=datetime('now') WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<Account>> GetDistributorsAsync() =>
        await GetByGroupCodeAsync("CREDITOR");

    public async Task<List<Account>> GetCustomersAsync() =>
        await GetByGroupCodeAsync("DEBTOR");

    public async Task<List<Account>> GetByGroupCodeAsync(string groupCode)
    {
        var list = new List<Account>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT a.*, ag.GroupName, ag.GroupCode FROM Accounts a
            JOIN AccountGroups ag ON ag.Id=a.GroupId
            WHERE ag.GroupCode=@gc AND a.TenantId=@tid AND a.IsDeleted=0 AND a.IsInactive=0
            ORDER BY a.AccountName";
        cmd.Parameters.AddWithValue("@gc", groupCode);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapAccount(r));
        return list;
    }

    public async Task<List<AccountGroup>> GetGroupsAsync()
    {
        var list = new List<AccountGroup>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM AccountGroups WHERE TenantId=@tid AND IsDeleted=0 ORDER BY GroupName";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new AccountGroup
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
                GroupCode = r.GetString(r.GetOrdinal("GroupCode")),
                GroupName = r.GetString(r.GetOrdinal("GroupName")),
                Level = r.GetInt32(r.GetOrdinal("Level")),
                NatureType = r.GetString(r.GetOrdinal("NatureType")),
                IsSystem = r.GetInt32(r.GetOrdinal("IsSystem")) == 1,
            });
        return list;
    }

    public async Task<decimal> GetOutstandingBalanceAsync(int accountId)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        // Sales - Receipts + Opening
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
                (SELECT COALESCE(SUM(NetAmount),0) FROM SaleMaster WHERE AccountId=@aid AND TenantId=@tid AND IsCancelled=0 AND IsDeleted=0) -
                (SELECT COALESCE(SUM(Amount),0) FROM ReceiptMaster WHERE AccountId=@aid AND TenantId=@tid AND IsCancelled=0 AND IsDeleted=0) +
                (SELECT CASE WHEN OpeningDr=1 THEN OpeningBalance ELSE -OpeningBalance END FROM Accounts WHERE Id=@aid AND TenantId=@tid)
            AS Outstanding";
        cmd.Parameters.AddWithValue("@aid", accountId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        var result = await cmd.ExecuteScalarAsync();
        return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
    }

    private void AddAccountParams(SqliteCommand cmd, Account a)
    {
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@code", a.AccountCode);
        cmd.Parameters.AddWithValue("@name", a.AccountName);
        cmd.Parameters.AddWithValue("@a1", a.Address1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@a2", a.Address2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@a3", a.Address3 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@a4", a.Address4 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@area", a.Area ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@city", a.City ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@state", a.State ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@stcode", a.StateCode ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@pin", a.PinCode ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@phone", a.Phone ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@mob", a.Mobile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@email", a.Email ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@gstin", a.GSTIN ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@grp", a.GroupId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@cd", a.CashDiscountPer);
        cmd.Parameters.AddWithValue("@bank", a.BankName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@bankac", a.BankAccountNo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ifsc", a.IFSCCode ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@dl", a.DrugLicenseNo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@pan", a.PANNo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@opbal", a.OpeningBalance);
        cmd.Parameters.AddWithValue("@opdr", a.OpeningDr ? 1 : 0);
        cmd.Parameters.AddWithValue("@due", a.DueDays);
        cmd.Parameters.AddWithValue("@locked", a.IsLocked ? 1 : 0);
        cmd.Parameters.AddWithValue("@inactive", a.IsInactive ? 1 : 0);
    }

    private static Account MapAccount(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
        AccountCode = r.GetString(r.GetOrdinal("AccountCode")),
        AccountName = r.GetString(r.GetOrdinal("AccountName")),
        Address1 = SafeStr(r, "Address1"),
        Address2 = SafeStr(r, "Address2"),
        Address3 = SafeStr(r, "Address3"),
        Address4 = SafeStr(r, "Address4"),
        Area = SafeStr(r, "Area"),
        City = SafeStr(r, "City"),
        State = SafeStr(r, "State"),
        StateCode = SafeStr(r, "StateCode"),
        PinCode = SafeStr(r, "PinCode"),
        Phone = SafeStr(r, "Phone"),
        Mobile = SafeStr(r, "Mobile"),
        Email = SafeStr(r, "Email"),
        GSTIN = SafeStr(r, "GSTIN"),
        GroupId = r.IsDBNull(r.GetOrdinal("GroupId")) ? null : r.GetInt32(r.GetOrdinal("GroupId")),
        CashDiscountPer = r.GetDecimal(r.GetOrdinal("CashDiscountPer")),
        BankName = SafeStr(r, "BankName"),
        BankAccountNo = SafeStr(r, "BankAccountNo"),
        IFSCCode = SafeStr(r, "IFSCCode"),
        DrugLicenseNo = SafeStr(r, "DrugLicenseNo"),
        PANNo = SafeStr(r, "PANNo"),
        OpeningBalance = r.GetDecimal(r.GetOrdinal("OpeningBalance")),
        OpeningDr = r.GetInt32(r.GetOrdinal("OpeningDr")) == 1,
        DueDays = r.GetInt32(r.GetOrdinal("DueDays")),
        IsLocked = r.GetInt32(r.GetOrdinal("IsLocked")) == 1,
        IsInactive = r.GetInt32(r.GetOrdinal("IsInactive")) == 1,
        IsDeleted = r.GetInt32(r.GetOrdinal("IsDeleted")) == 1,
        GroupName = SafeStr(r, "GroupName"),
        GroupCode = SafeStr(r, "GroupCode"),
    };

    private static string? SafeStr(SqliteDataReader r, string col)
    {
        try { var i = r.GetOrdinal(col); return r.IsDBNull(i) ? null : r.GetString(i); }
        catch { return null; }
    }
}
