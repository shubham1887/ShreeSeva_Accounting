using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly IDbConnectionFactory _db;
    public PatientRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<Patient>> GetAllAsync() => await SearchAsync(null);

    public async Task<List<Patient>> SearchAsync(string? searchTerm, int? doctorId = null)
    {
        var list = new List<Patient>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        var where = "p.TenantId=@tid AND p.IsDeleted=0";
        if (!string.IsNullOrWhiteSpace(searchTerm))
            where += " AND (p.PatientName LIKE @s OR p.Mobile LIKE @s OR p.Phone LIKE @s)";
        if (doctorId.HasValue) where += " AND p.DoctorId=@did";
        cmd.CommandText = $@"SELECT p.*, d.DoctorName FROM Patients p
            LEFT JOIN Doctors d ON d.Id=p.DoctorId
            WHERE {where} ORDER BY p.PatientName LIMIT 100";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@s", $"%{searchTerm}%");
        cmd.Parameters.AddWithValue("@did", doctorId ?? (object)DBNull.Value);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapPatient(r));
        return list;
    }

    public async Task<Patient?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT p.*, d.DoctorName FROM Patients p
            LEFT JOIN Doctors d ON d.Id=p.DoctorId
            WHERE p.Id=@id AND p.TenantId=@tid AND p.IsDeleted=0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapPatient(r) : null;
    }

    public async Task<int> CreateAsync(Patient patient)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Patients(TenantId,PatientCode,PatientName,Address1,Address2,Phone,Mobile,Email,DoctorId,BloodGroup,DateOfBirth,Gender,CreatedAt,UpdatedAt)
            VALUES(@tid,@code,@name,@a1,@a2,@ph,@mob,@email,@did,@bg,@dob,@gender,datetime('now'),datetime('now'));
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@code", patient.PatientCode);
        cmd.Parameters.AddWithValue("@name", patient.PatientName);
        cmd.Parameters.AddWithValue("@a1", patient.Address1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@a2", patient.Address2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ph", patient.Phone ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@mob", patient.Mobile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@email", patient.Email ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@did", patient.DoctorId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@bg", patient.BloodGroup ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@dob", patient.DateOfBirth ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@gender", patient.Gender ?? (object)DBNull.Value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<bool> UpdateAsync(Patient patient)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Patients SET PatientCode=@code,PatientName=@name,Address1=@a1,Address2=@a2,
            Phone=@ph,Mobile=@mob,Email=@email,DoctorId=@did,BloodGroup=@bg,DateOfBirth=@dob,Gender=@gender,
            UpdatedAt=datetime('now') WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@code", patient.PatientCode);
        cmd.Parameters.AddWithValue("@name", patient.PatientName);
        cmd.Parameters.AddWithValue("@a1", patient.Address1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@a2", patient.Address2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ph", patient.Phone ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@mob", patient.Mobile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@email", patient.Email ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@did", patient.DoctorId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@bg", patient.BloodGroup ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@dob", patient.DateOfBirth ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@gender", patient.Gender ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@id", patient.Id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Patients SET IsDeleted=1,UpdatedAt=datetime('now') WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static Patient MapPatient(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
        PatientCode = r.GetString(r.GetOrdinal("PatientCode")),
        PatientName = r.GetString(r.GetOrdinal("PatientName")),
        Address1 = r.IsDBNull(r.GetOrdinal("Address1")) ? null : r.GetString(r.GetOrdinal("Address1")),
        Address2 = r.IsDBNull(r.GetOrdinal("Address2")) ? null : r.GetString(r.GetOrdinal("Address2")),
        Phone = r.IsDBNull(r.GetOrdinal("Phone")) ? null : r.GetString(r.GetOrdinal("Phone")),
        Mobile = r.IsDBNull(r.GetOrdinal("Mobile")) ? null : r.GetString(r.GetOrdinal("Mobile")),
        Email = r.IsDBNull(r.GetOrdinal("Email")) ? null : r.GetString(r.GetOrdinal("Email")),
        DoctorId = r.IsDBNull(r.GetOrdinal("DoctorId")) ? null : r.GetInt32(r.GetOrdinal("DoctorId")),
        BloodGroup = r.IsDBNull(r.GetOrdinal("BloodGroup")) ? null : r.GetString(r.GetOrdinal("BloodGroup")),
        DateOfBirth = r.IsDBNull(r.GetOrdinal("DateOfBirth")) ? null : r.GetString(r.GetOrdinal("DateOfBirth")),
        Gender = r.IsDBNull(r.GetOrdinal("Gender")) ? null : r.GetString(r.GetOrdinal("Gender")),
        IsDeleted = r.GetInt32(r.GetOrdinal("IsDeleted")) == 1,
        DoctorName = SafeStr(r, "DoctorName"),
    };

    private static string? SafeStr(SqliteDataReader r, string col)
    {
        try { var i = r.GetOrdinal(col); return r.IsDBNull(i) ? null : r.GetString(i); }
        catch { return null; }
    }
}
