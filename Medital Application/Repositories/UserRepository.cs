using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _db;
    public UserRepository(IDbConnectionFactory db) => _db = db;

    public async Task<User?> GetByCodeAsync(string userCode)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Users WHERE UserCode=@code AND TenantId=@tid AND IsDeleted=0 LIMIT 1";
        cmd.Parameters.AddWithValue("@code", userCode);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapUser(r) : null;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Users WHERE Id=@id AND TenantId=@tid AND IsDeleted=0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapUser(r) : null;
    }

    public async Task<List<User>> GetAllAsync()
    {
        var list = new List<User>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Users WHERE TenantId=@tid AND IsDeleted=0 ORDER BY UserName";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapUser(r));
        return list;
    }

    public async Task<int> CreateAsync(User user, UserRight rights)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO Users(TenantId,UserCode,UserName,PasswordHash,Mobile,Email,JoinDate,IsAdmin,IsActive)
                VALUES(@tid,@code,@name,@hash,@mob,@email,@join,@admin,@active);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@tid", _db.TenantId);
            cmd.Parameters.AddWithValue("@code", user.UserCode);
            cmd.Parameters.AddWithValue("@name", user.UserName);
            cmd.Parameters.AddWithValue("@hash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@mob", user.Mobile ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@email", user.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@join", user.JoinDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@admin", user.IsAdmin ? 1 : 0);
            cmd.Parameters.AddWithValue("@active", user.IsActive ? 1 : 0);
            var userId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            rights.UserId = userId;
            rights.TenantId = _db.TenantId;
            using var rCmd = conn.CreateCommand();
            rCmd.Transaction = tx;
            rCmd.CommandText = @"INSERT INTO UserRights(TenantId,UserId,CanSale,CanSaleEdit,CanSaleDelete,
                CanPurchase,CanPurchaseEdit,CanPurchaseDelete,CanReceipt,CanPayment,CanCreditNote,CanDebitNote,
                CanJournal,CanStockAdjust,CanProductMaster,CanAccountMaster,CanDoctorMaster,CanPatientMaster,
                CanReports,CanGSTReports,CanUserMgmt,CanSettings,CanBackup,CanDayClose,CanViewCost,CanChangeRate,
                CanGiveDiscount,MaxDiscountPer)
                VALUES(@tid,@uid,@s,@se,@sd,@p,@pe,@pd,@rc,@py,@cn,@dn,
                @j,@sa,@pm,@am,@dm,@ptm,@rp,@gst,@um,@set,@bk,@dc,@vc,@cr,@gd,@md)";
            rCmd.Parameters.AddWithValue("@tid", _db.TenantId);
            rCmd.Parameters.AddWithValue("@uid", userId);
            rCmd.Parameters.AddWithValue("@s", rights.CanSale ? 1 : 0);
            rCmd.Parameters.AddWithValue("@se", rights.CanSaleEdit ? 1 : 0);
            rCmd.Parameters.AddWithValue("@sd", rights.CanSaleDelete ? 1 : 0);
            rCmd.Parameters.AddWithValue("@p", rights.CanPurchase ? 1 : 0);
            rCmd.Parameters.AddWithValue("@pe", rights.CanPurchaseEdit ? 1 : 0);
            rCmd.Parameters.AddWithValue("@pd", rights.CanPurchaseDelete ? 1 : 0);
            rCmd.Parameters.AddWithValue("@rc", rights.CanReceipt ? 1 : 0);
            rCmd.Parameters.AddWithValue("@py", rights.CanPayment ? 1 : 0);
            rCmd.Parameters.AddWithValue("@cn", rights.CanCreditNote ? 1 : 0);
            rCmd.Parameters.AddWithValue("@dn", rights.CanDebitNote ? 1 : 0);
            rCmd.Parameters.AddWithValue("@j", rights.CanJournal ? 1 : 0);
            rCmd.Parameters.AddWithValue("@sa", rights.CanStockAdjust ? 1 : 0);
            rCmd.Parameters.AddWithValue("@pm", rights.CanProductMaster ? 1 : 0);
            rCmd.Parameters.AddWithValue("@am", rights.CanAccountMaster ? 1 : 0);
            rCmd.Parameters.AddWithValue("@dm", rights.CanDoctorMaster ? 1 : 0);
            rCmd.Parameters.AddWithValue("@ptm", rights.CanPatientMaster ? 1 : 0);
            rCmd.Parameters.AddWithValue("@rp", rights.CanReports ? 1 : 0);
            rCmd.Parameters.AddWithValue("@gst", rights.CanGSTReports ? 1 : 0);
            rCmd.Parameters.AddWithValue("@um", rights.CanUserMgmt ? 1 : 0);
            rCmd.Parameters.AddWithValue("@set", rights.CanSettings ? 1 : 0);
            rCmd.Parameters.AddWithValue("@bk", rights.CanBackup ? 1 : 0);
            rCmd.Parameters.AddWithValue("@dc", rights.CanDayClose ? 1 : 0);
            rCmd.Parameters.AddWithValue("@vc", rights.CanViewCost ? 1 : 0);
            rCmd.Parameters.AddWithValue("@cr", rights.CanChangeRate ? 1 : 0);
            rCmd.Parameters.AddWithValue("@gd", rights.CanGiveDiscount ? 1 : 0);
            rCmd.Parameters.AddWithValue("@md", rights.MaxDiscountPer);
            await rCmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
            return userId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(User user)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Users SET UserCode=@code,UserName=@name,Mobile=@mob,Email=@email,
            IsAdmin=@admin,IsActive=@active,UpdatedAt=datetime('now')
            WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@code", user.UserCode);
        cmd.Parameters.AddWithValue("@name", user.UserName);
        cmd.Parameters.AddWithValue("@mob", user.Mobile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@email", user.Email ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@admin", user.IsAdmin ? 1 : 0);
        cmd.Parameters.AddWithValue("@active", user.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@id", user.Id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string newPasswordHash)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Users SET PasswordHash=@hash,UpdatedAt=datetime('now') WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@hash", newPasswordHash);
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateRightsAsync(UserRight rights)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE UserRights SET CanSale=@s,CanSaleEdit=@se,CanSaleDelete=@sd,
            CanPurchase=@p,CanPurchaseEdit=@pe,CanPurchaseDelete=@pd,CanReceipt=@rc,CanPayment=@py,
            CanCreditNote=@cn,CanDebitNote=@dn,CanJournal=@j,CanStockAdjust=@sa,CanProductMaster=@pm,
            CanAccountMaster=@am,CanDoctorMaster=@dm,CanPatientMaster=@ptm,CanReports=@rp,CanGSTReports=@gst,
            CanUserMgmt=@um,CanSettings=@set,CanBackup=@bk,CanDayClose=@dc,CanViewCost=@vc,CanChangeRate=@cr,
            CanGiveDiscount=@gd,MaxDiscountPer=@md,UpdatedAt=datetime('now')
            WHERE UserId=@uid AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@s", rights.CanSale ? 1 : 0);
        cmd.Parameters.AddWithValue("@se", rights.CanSaleEdit ? 1 : 0);
        cmd.Parameters.AddWithValue("@sd", rights.CanSaleDelete ? 1 : 0);
        cmd.Parameters.AddWithValue("@p", rights.CanPurchase ? 1 : 0);
        cmd.Parameters.AddWithValue("@pe", rights.CanPurchaseEdit ? 1 : 0);
        cmd.Parameters.AddWithValue("@pd", rights.CanPurchaseDelete ? 1 : 0);
        cmd.Parameters.AddWithValue("@rc", rights.CanReceipt ? 1 : 0);
        cmd.Parameters.AddWithValue("@py", rights.CanPayment ? 1 : 0);
        cmd.Parameters.AddWithValue("@cn", rights.CanCreditNote ? 1 : 0);
        cmd.Parameters.AddWithValue("@dn", rights.CanDebitNote ? 1 : 0);
        cmd.Parameters.AddWithValue("@j", rights.CanJournal ? 1 : 0);
        cmd.Parameters.AddWithValue("@sa", rights.CanStockAdjust ? 1 : 0);
        cmd.Parameters.AddWithValue("@pm", rights.CanProductMaster ? 1 : 0);
        cmd.Parameters.AddWithValue("@am", rights.CanAccountMaster ? 1 : 0);
        cmd.Parameters.AddWithValue("@dm", rights.CanDoctorMaster ? 1 : 0);
        cmd.Parameters.AddWithValue("@ptm", rights.CanPatientMaster ? 1 : 0);
        cmd.Parameters.AddWithValue("@rp", rights.CanReports ? 1 : 0);
        cmd.Parameters.AddWithValue("@gst", rights.CanGSTReports ? 1 : 0);
        cmd.Parameters.AddWithValue("@um", rights.CanUserMgmt ? 1 : 0);
        cmd.Parameters.AddWithValue("@set", rights.CanSettings ? 1 : 0);
        cmd.Parameters.AddWithValue("@bk", rights.CanBackup ? 1 : 0);
        cmd.Parameters.AddWithValue("@dc", rights.CanDayClose ? 1 : 0);
        cmd.Parameters.AddWithValue("@vc", rights.CanViewCost ? 1 : 0);
        cmd.Parameters.AddWithValue("@cr", rights.CanChangeRate ? 1 : 0);
        cmd.Parameters.AddWithValue("@gd", rights.CanGiveDiscount ? 1 : 0);
        cmd.Parameters.AddWithValue("@md", rights.MaxDiscountPer);
        cmd.Parameters.AddWithValue("@uid", rights.UserId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<UserRight?> GetRightsAsync(int userId)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM UserRights WHERE UserId=@uid AND TenantId=@tid AND IsDeleted=0";
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new UserRight
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
            UserId = r.GetInt32(r.GetOrdinal("UserId")),
            CanSale = r.GetInt32(r.GetOrdinal("CanSale")) == 1,
            CanSaleEdit = r.GetInt32(r.GetOrdinal("CanSaleEdit")) == 1,
            CanSaleDelete = r.GetInt32(r.GetOrdinal("CanSaleDelete")) == 1,
            CanPurchase = r.GetInt32(r.GetOrdinal("CanPurchase")) == 1,
            CanPurchaseEdit = r.GetInt32(r.GetOrdinal("CanPurchaseEdit")) == 1,
            CanPurchaseDelete = r.GetInt32(r.GetOrdinal("CanPurchaseDelete")) == 1,
            CanReceipt = r.GetInt32(r.GetOrdinal("CanReceipt")) == 1,
            CanPayment = r.GetInt32(r.GetOrdinal("CanPayment")) == 1,
            CanCreditNote = r.GetInt32(r.GetOrdinal("CanCreditNote")) == 1,
            CanDebitNote = r.GetInt32(r.GetOrdinal("CanDebitNote")) == 1,
            CanJournal = r.GetInt32(r.GetOrdinal("CanJournal")) == 1,
            CanStockAdjust = r.GetInt32(r.GetOrdinal("CanStockAdjust")) == 1,
            CanProductMaster = r.GetInt32(r.GetOrdinal("CanProductMaster")) == 1,
            CanAccountMaster = r.GetInt32(r.GetOrdinal("CanAccountMaster")) == 1,
            CanDoctorMaster = r.GetInt32(r.GetOrdinal("CanDoctorMaster")) == 1,
            CanPatientMaster = r.GetInt32(r.GetOrdinal("CanPatientMaster")) == 1,
            CanReports = r.GetInt32(r.GetOrdinal("CanReports")) == 1,
            CanGSTReports = r.GetInt32(r.GetOrdinal("CanGSTReports")) == 1,
            CanUserMgmt = r.GetInt32(r.GetOrdinal("CanUserMgmt")) == 1,
            CanSettings = r.GetInt32(r.GetOrdinal("CanSettings")) == 1,
            CanBackup = r.GetInt32(r.GetOrdinal("CanBackup")) == 1,
            CanDayClose = r.GetInt32(r.GetOrdinal("CanDayClose")) == 1,
            CanViewCost = r.GetInt32(r.GetOrdinal("CanViewCost")) == 1,
            CanChangeRate = r.GetInt32(r.GetOrdinal("CanChangeRate")) == 1,
            CanGiveDiscount = r.GetInt32(r.GetOrdinal("CanGiveDiscount")) == 1,
            MaxDiscountPer = r.GetDecimal(r.GetOrdinal("MaxDiscountPer")),
        };
    }

    public async Task<bool> ValidatePasswordAsync(string userCode, string passwordHash)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE UserCode=@code AND PasswordHash=@hash AND TenantId=@tid AND IsDeleted=0 AND IsActive=1";
        cmd.Parameters.AddWithValue("@code", userCode);
        cmd.Parameters.AddWithValue("@hash", passwordHash);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    private static User MapUser(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
        UserCode = r.GetString(r.GetOrdinal("UserCode")),
        UserName = r.GetString(r.GetOrdinal("UserName")),
        PasswordHash = r.GetString(r.GetOrdinal("PasswordHash")),
        Mobile = r.IsDBNull(r.GetOrdinal("Mobile")) ? null : r.GetString(r.GetOrdinal("Mobile")),
        Email = r.IsDBNull(r.GetOrdinal("Email")) ? null : r.GetString(r.GetOrdinal("Email")),
        JoinDate = r.IsDBNull(r.GetOrdinal("JoinDate")) ? null : r.GetString(r.GetOrdinal("JoinDate")),
        IsAdmin = r.GetInt32(r.GetOrdinal("IsAdmin")) == 1,
        IsActive = r.GetInt32(r.GetOrdinal("IsActive")) == 1,
        IsDeleted = r.GetInt32(r.GetOrdinal("IsDeleted")) == 1,
    };
}
