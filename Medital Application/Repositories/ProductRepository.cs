using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Requests;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _db;

    public ProductRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<Product>> GetAllAsync()
    {
        var list = new List<Product>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT p.*, m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Products p
            LEFT JOIN Manufacturers m ON m.Id = p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id = p.DrugCategoryId
            WHERE p.TenantId=@tid AND p.IsDeleted=0
            ORDER BY p.ProductName";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapProduct(r));
        return list;
    }

    public async Task<List<Product>> SearchAsync(SearchProductRequest request)
    {
        var list = new List<Product>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        var where = "p.TenantId=@tid AND p.IsDeleted=0";
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            where += " AND (p.ProductName LIKE @s OR p.Barcode=@sExact OR p.ProductCode LIKE @s)";
        if (request.CategoryId.HasValue)
            where += " AND p.DrugCategoryId=@cat";
        if (request.ManufacturerId.HasValue)
            where += " AND p.ManufacturerId=@mfr";
        if (request.OnlyInStock)
            where += " AND p.CurrentQty > 0";

        var offset = (request.PageNo - 1) * request.PageSize;
        cmd.CommandText = $@"
            SELECT p.*, m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Products p
            LEFT JOIN Manufacturers m ON m.Id = p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id = p.DrugCategoryId
            WHERE {where}
            ORDER BY p.ProductName
            LIMIT @limit OFFSET @offset";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@s", $"%{request.SearchTerm}%");
        cmd.Parameters.AddWithValue("@sExact", request.SearchTerm ?? "");
        cmd.Parameters.AddWithValue("@cat", request.CategoryId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@mfr", request.ManufacturerId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@limit", request.PageSize);
        cmd.Parameters.AddWithValue("@offset", offset);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapProduct(r));
        return list;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT p.*, m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Products p
            LEFT JOIN Manufacturers m ON m.Id = p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id = p.DrugCategoryId
            WHERE p.Id=@id AND p.TenantId=@tid AND p.IsDeleted=0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapProduct(r) : null;
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT p.*, m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Products p
            LEFT JOIN Manufacturers m ON m.Id = p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id = p.DrugCategoryId
            WHERE p.Barcode=@bc AND p.TenantId=@tid AND p.IsDeleted=0 LIMIT 1";
        cmd.Parameters.AddWithValue("@bc", barcode);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapProduct(r) : null;
    }

    public async Task<Product?> GetByCodeAsync(string code)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT p.*, m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Products p
            LEFT JOIN Manufacturers m ON m.Id = p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id = p.DrugCategoryId
            WHERE p.ProductCode=@code AND p.TenantId=@tid AND p.IsDeleted=0 LIMIT 1";
        cmd.Parameters.AddWithValue("@code", code);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapProduct(r) : null;
    }

    public async Task<List<Product>> GetLowStockAsync()
    {
        var list = new List<Product>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT p.*, m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Products p
            LEFT JOIN Manufacturers m ON m.Id = p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id = p.DrugCategoryId
            WHERE p.TenantId=@tid AND p.IsDeleted=0 AND p.MinQty > 0 AND p.CurrentQty <= p.MinQty
            ORDER BY p.CurrentQty";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapProduct(r));
        return list;
    }

    public async Task<int> CreateAsync(Product product)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Products
            (TenantId,ProductCode,ProductName,MarathiName,Barcode,Unit,PackSize,ManufacturerId,DrugCategoryId,
             HSNCode,SGSTRate,CGSTRate,IGSTRate,IsFixedRate,Margin,MinQty,MaxQty,IsNonRx,IsScheduled,IsHighRisk,
             DefaultSaleRate,DefaultMRP,LastPurchaseRate,CurrentQty,CreatedAt,UpdatedAt)
            VALUES
            (@tid,@code,@name,@mname,@bc,@unit,@pack,@mfr,@cat,
             @hsn,@sgst,@cgst,@igst,@fix,@margin,@min,@max,@nrx,@sched,@high,
             @salerate,@mrp,@purrate,@qty,datetime('now'),datetime('now'));
            SELECT last_insert_rowid();";
        AddProductParams(cmd, product);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Products SET
            ProductCode=@code,ProductName=@name,MarathiName=@mname,Barcode=@bc,Unit=@unit,PackSize=@pack,
            ManufacturerId=@mfr,DrugCategoryId=@cat,HSNCode=@hsn,SGSTRate=@sgst,CGSTRate=@cgst,IGSTRate=@igst,
            IsFixedRate=@fix,Margin=@margin,MinQty=@min,MaxQty=@max,IsNonRx=@nrx,IsScheduled=@sched,IsHighRisk=@high,
            DefaultSaleRate=@salerate,DefaultMRP=@mrp,LastPurchaseRate=@purrate,CurrentQty=@qty,UpdatedAt=datetime('now')
            WHERE Id=@id AND TenantId=@tid";
        AddProductParams(cmd, product);
        cmd.Parameters.AddWithValue("@id", product.Id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Products SET IsDeleted=1,UpdatedAt=datetime('now') WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateStockQtyAsync(int productId, decimal qty)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Products SET CurrentQty=@qty,UpdatedAt=datetime('now')
                            WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@qty", qty);
        cmd.Parameters.AddWithValue("@id", productId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private void AddProductParams(SqliteCommand cmd, Product p)
    {
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@code", p.ProductCode);
        cmd.Parameters.AddWithValue("@name", p.ProductName);
        cmd.Parameters.AddWithValue("@mname", p.MarathiName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@bc", p.Barcode ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@unit", p.Unit);
        cmd.Parameters.AddWithValue("@pack", p.PackSize);
        cmd.Parameters.AddWithValue("@mfr", p.ManufacturerId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@cat", p.DrugCategoryId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@hsn", p.HSNCode ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@sgst", p.SGSTRate);
        cmd.Parameters.AddWithValue("@cgst", p.CGSTRate);
        cmd.Parameters.AddWithValue("@igst", p.IGSTRate);
        cmd.Parameters.AddWithValue("@fix", p.IsFixedRate ? 1 : 0);
        cmd.Parameters.AddWithValue("@margin", p.Margin);
        cmd.Parameters.AddWithValue("@min", p.MinQty);
        cmd.Parameters.AddWithValue("@max", p.MaxQty);
        cmd.Parameters.AddWithValue("@nrx", p.IsNonRx ? 1 : 0);
        cmd.Parameters.AddWithValue("@sched", p.IsScheduled ? 1 : 0);
        cmd.Parameters.AddWithValue("@high", p.IsHighRisk ? 1 : 0);
        cmd.Parameters.AddWithValue("@salerate", p.DefaultSaleRate);
        cmd.Parameters.AddWithValue("@mrp", p.DefaultMRP);
        cmd.Parameters.AddWithValue("@purrate", p.LastPurchaseRate);
        cmd.Parameters.AddWithValue("@qty", p.CurrentQty);
    }

    private static Product MapProduct(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
        ProductCode = r.GetString(r.GetOrdinal("ProductCode")),
        ProductName = r.GetString(r.GetOrdinal("ProductName")),
        MarathiName = r.IsDBNull(r.GetOrdinal("MarathiName")) ? null : r.GetString(r.GetOrdinal("MarathiName")),
        Barcode = r.IsDBNull(r.GetOrdinal("Barcode")) ? null : r.GetString(r.GetOrdinal("Barcode")),
        Unit = r.GetString(r.GetOrdinal("Unit")),
        PackSize = r.GetInt32(r.GetOrdinal("PackSize")),
        ManufacturerId = r.IsDBNull(r.GetOrdinal("ManufacturerId")) ? null : r.GetInt32(r.GetOrdinal("ManufacturerId")),
        DrugCategoryId = r.IsDBNull(r.GetOrdinal("DrugCategoryId")) ? null : r.GetInt32(r.GetOrdinal("DrugCategoryId")),
        HSNCode = r.IsDBNull(r.GetOrdinal("HSNCode")) ? null : r.GetString(r.GetOrdinal("HSNCode")),
        SGSTRate = r.GetDecimal(r.GetOrdinal("SGSTRate")),
        CGSTRate = r.GetDecimal(r.GetOrdinal("CGSTRate")),
        IGSTRate = r.GetDecimal(r.GetOrdinal("IGSTRate")),
        IsFixedRate = r.GetInt32(r.GetOrdinal("IsFixedRate")) == 1,
        Margin = r.GetDecimal(r.GetOrdinal("Margin")),
        MinQty = r.GetDecimal(r.GetOrdinal("MinQty")),
        MaxQty = r.GetDecimal(r.GetOrdinal("MaxQty")),
        IsNonRx = r.GetInt32(r.GetOrdinal("IsNonRx")) == 1,
        IsScheduled = r.GetInt32(r.GetOrdinal("IsScheduled")) == 1,
        IsHighRisk = r.GetInt32(r.GetOrdinal("IsHighRisk")) == 1,
        DefaultSaleRate = r.GetDecimal(r.GetOrdinal("DefaultSaleRate")),
        DefaultMRP = r.GetDecimal(r.GetOrdinal("DefaultMRP")),
        LastPurchaseRate = r.GetDecimal(r.GetOrdinal("LastPurchaseRate")),
        CurrentQty = r.GetDecimal(r.GetOrdinal("CurrentQty")),
        IsDeleted = r.GetInt32(r.GetOrdinal("IsDeleted")) == 1,
        ManufacturerName = SafeString(r, "ManufacturerName"),
        CategoryName = SafeString(r, "CategoryName"),
    };

    private static string? SafeString(SqliteDataReader r, string col)
    {
        try { return r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetString(r.GetOrdinal(col)); }
        catch { return null; }
    }
}
