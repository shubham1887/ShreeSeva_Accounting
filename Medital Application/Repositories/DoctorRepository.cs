using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly IDbConnectionFactory _db;
    public DoctorRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<Doctor>> GetAllAsync() => await SearchAsync(null);

    public async Task<List<Doctor>> SearchAsync(string? searchTerm)
    {
        var list = new List<Doctor>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        var where = "TenantId=@tid AND IsDeleted=0";
        if (!string.IsNullOrWhiteSpace(searchTerm))
            where += " AND (DoctorName LIKE @s OR Mobile LIKE @s OR Phone LIKE @s)";
        cmd.CommandText = $"SELECT * FROM Doctors WHERE {where} ORDER BY DoctorName";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@s", $"%{searchTerm}%");
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapDoctor(r));
        return list;
    }

    public async Task<Doctor?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Doctors WHERE Id=@id AND TenantId=@tid AND IsDeleted=0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapDoctor(r) : null;
    }

    public async Task<int> CreateAsync(Doctor doctor)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Doctors(TenantId,DoctorCode,DoctorName,Address,Phone,Mobile,RegNo,IncentivePer,CreatedAt,UpdatedAt)
            VALUES(@tid,@code,@name,@addr,@ph,@mob,@reg,@inc,datetime('now'),datetime('now'));
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@code", doctor.DoctorCode);
        cmd.Parameters.AddWithValue("@name", doctor.DoctorName);
        cmd.Parameters.AddWithValue("@addr", doctor.Address ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ph", doctor.Phone ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@mob", doctor.Mobile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@reg", doctor.RegNo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@inc", doctor.IncentivePer);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<bool> UpdateAsync(Doctor doctor)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Doctors SET DoctorCode=@code,DoctorName=@name,Address=@addr,
            Phone=@ph,Mobile=@mob,RegNo=@reg,IncentivePer=@inc,UpdatedAt=datetime('now')
            WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@code", doctor.DoctorCode);
        cmd.Parameters.AddWithValue("@name", doctor.DoctorName);
        cmd.Parameters.AddWithValue("@addr", doctor.Address ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ph", doctor.Phone ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@mob", doctor.Mobile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@reg", doctor.RegNo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@inc", doctor.IncentivePer);
        cmd.Parameters.AddWithValue("@id", doctor.Id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Doctors SET IsDeleted=1,UpdatedAt=datetime('now') WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static Doctor MapDoctor(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
        DoctorCode = r.GetString(r.GetOrdinal("DoctorCode")),
        DoctorName = r.GetString(r.GetOrdinal("DoctorName")),
        Address = r.IsDBNull(r.GetOrdinal("Address")) ? null : r.GetString(r.GetOrdinal("Address")),
        Phone = r.IsDBNull(r.GetOrdinal("Phone")) ? null : r.GetString(r.GetOrdinal("Phone")),
        Mobile = r.IsDBNull(r.GetOrdinal("Mobile")) ? null : r.GetString(r.GetOrdinal("Mobile")),
        RegNo = r.IsDBNull(r.GetOrdinal("RegNo")) ? null : r.GetString(r.GetOrdinal("RegNo")),
        IncentivePer = r.GetDecimal(r.GetOrdinal("IncentivePer")),
        IsDeleted = r.GetInt32(r.GetOrdinal("IsDeleted")) == 1,
    };
}
