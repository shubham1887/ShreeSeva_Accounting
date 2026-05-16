using Medital_Application.Data;
using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class StockRepository : IStockRepository
{
    private readonly IDbConnectionFactory _db;
    public StockRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<Stock>> GetByProductAsync(int productId)
    {
        var list = new List<Stock>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT s.*, p.ProductName, p.ProductCode, p.HSNCode,
            m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Stocks s
            JOIN Products p ON p.Id=s.ProductId
            LEFT JOIN Manufacturers m ON m.Id=p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id=p.DrugCategoryId
            WHERE s.ProductId=@pid AND s.TenantId=@tid AND s.IsDeleted=0
            ORDER BY s.ExpiryDate";
        cmd.Parameters.AddWithValue("@pid", productId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapStock(r));
        return list;
    }

    public async Task<Stock?> GetByKeyAsync(string stockKey)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT s.*, p.ProductName, p.ProductCode, p.HSNCode,
            m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Stocks s
            JOIN Products p ON p.Id=s.ProductId
            LEFT JOIN Manufacturers m ON m.Id=p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id=p.DrugCategoryId
            WHERE s.StockKey=@key AND s.TenantId=@tid AND s.IsDeleted=0 LIMIT 1";
        cmd.Parameters.AddWithValue("@key", stockKey);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapStock(r) : null;
    }

    public async Task<Stock?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT s.*, p.ProductName, p.ProductCode, p.HSNCode,
            m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Stocks s
            JOIN Products p ON p.Id=s.ProductId
            LEFT JOIN Manufacturers m ON m.Id=p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id=p.DrugCategoryId
            WHERE s.Id=@id AND s.TenantId=@tid AND s.IsDeleted=0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapStock(r) : null;
    }

    public async Task<List<Stock>> GetAvailableBatchesAsync(int productId)
    {
        var list = new List<Stock>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        // FIFO: oldest expiry first, only positive current qty
        cmd.CommandText = @"SELECT s.*, p.ProductName, p.ProductCode, p.HSNCode,
            m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Stocks s
            JOIN Products p ON p.Id=s.ProductId
            LEFT JOIN Manufacturers m ON m.Id=p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id=p.DrugCategoryId
            WHERE s.ProductId=@pid AND s.TenantId=@tid AND s.IsDeleted=0
              AND (s.OpeningQty+s.PurchasedQty+s.CreditNoteQty+s.StockInQty-s.SoldQty-s.StockOutQty) > 0
            ORDER BY s.ExpiryDate ASC";
        cmd.Parameters.AddWithValue("@pid", productId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapStock(r));
        return list;
    }

    public async Task<List<Stock>> GetExpiringAsync(int monthsAhead = 3)
    {
        var list = new List<Stock>();
        var cutoff = DateTime.Today.AddMonths(monthsAhead).ToString("yyyy-MM-dd");
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT s.*, p.ProductName, p.ProductCode, p.HSNCode,
            m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Stocks s
            JOIN Products p ON p.Id=s.ProductId
            LEFT JOIN Manufacturers m ON m.Id=p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id=p.DrugCategoryId
            WHERE s.TenantId=@tid AND s.IsDeleted=0
              AND s.ExpiryDate >= @today AND s.ExpiryDate <= @cutoff
              AND (s.OpeningQty+s.PurchasedQty+s.CreditNoteQty+s.StockInQty-s.SoldQty-s.StockOutQty) > 0
            ORDER BY s.ExpiryDate";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@today", today);
        cmd.Parameters.AddWithValue("@cutoff", cutoff);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapStock(r));
        return list;
    }

    public async Task<List<Stock>> GetExpiredAsync()
    {
        var list = new List<Stock>();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT s.*, p.ProductName, p.ProductCode, p.HSNCode,
            m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Stocks s
            JOIN Products p ON p.Id=s.ProductId
            LEFT JOIN Manufacturers m ON m.Id=p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id=p.DrugCategoryId
            WHERE s.TenantId=@tid AND s.IsDeleted=0 AND s.ExpiryDate < @today
              AND (s.OpeningQty+s.PurchasedQty+s.CreditNoteQty+s.StockInQty-s.SoldQty-s.StockOutQty) > 0
            ORDER BY s.ExpiryDate";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@today", today);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapStock(r));
        return list;
    }

    public async Task<List<Stock>> GetLowStockAsync()
    {
        var list = new List<Stock>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT s.*, p.ProductName, p.ProductCode, p.HSNCode, p.MinQty,
            m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Stocks s
            JOIN Products p ON p.Id=s.ProductId
            LEFT JOIN Manufacturers m ON m.Id=p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id=p.DrugCategoryId
            WHERE s.TenantId=@tid AND s.IsDeleted=0 AND p.MinQty > 0
              AND p.CurrentQty <= p.MinQty
            ORDER BY p.CurrentQty";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapStock(r));
        return list;
    }

    public async Task<List<Stock>> GetAllAsync(string? searchTerm = null, int? categoryId = null, int? manufacturerId = null)
    {
        var list = new List<Stock>();
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        var where = "s.TenantId=@tid AND s.IsDeleted=0 AND p.IsDeleted=0";
        if (!string.IsNullOrWhiteSpace(searchTerm))
            where += " AND (p.ProductName LIKE @s OR p.Barcode=@sEx OR s.BatchNo LIKE @s)";
        if (categoryId.HasValue)
            where += " AND p.DrugCategoryId=@cat";
        if (manufacturerId.HasValue)
            where += " AND p.ManufacturerId=@mfr";

        cmd.CommandText = $@"SELECT s.*, p.ProductName, p.ProductCode, p.HSNCode, p.MinQty,
            m.CompanyName AS ManufacturerName, dc.CategoryName
            FROM Stocks s
            JOIN Products p ON p.Id=s.ProductId
            LEFT JOIN Manufacturers m ON m.Id=p.ManufacturerId
            LEFT JOIN DrugCategories dc ON dc.Id=p.DrugCategoryId
            WHERE {where}
            ORDER BY p.ProductName, s.ExpiryDate";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@s", $"%{searchTerm}%");
        cmd.Parameters.AddWithValue("@sEx", searchTerm ?? "");
        cmd.Parameters.AddWithValue("@cat", categoryId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@mfr", manufacturerId ?? (object)DBNull.Value);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapStock(r));
        return list;
    }

    public async Task<int> CreateAsync(Stock stock)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Stocks
            (TenantId,ProductId,BatchNo,ExpiryMY,ExpiryDate,GodownCode,ActualRate,NetRate,MRP,SaleRate,
             OpeningQty,PurchasedQty,SoldQty,CreditNoteQty,StockInQty,StockOutQty,StockKey,CreatedAt,UpdatedAt)
            VALUES(@tid,@pid,@batch,@expmy,@expdt,@god,@ar,@nr,@mrp,@sr,
             @opq,@purq,@salq,@cnq,@stiq,@stoq,@key,datetime('now'),datetime('now'));
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@pid", stock.ProductId);
        cmd.Parameters.AddWithValue("@batch", stock.BatchNo);
        cmd.Parameters.AddWithValue("@expmy", stock.ExpiryMY);
        cmd.Parameters.AddWithValue("@expdt", stock.ExpiryDate);
        cmd.Parameters.AddWithValue("@god", stock.GodownCode);
        cmd.Parameters.AddWithValue("@ar", stock.ActualRate);
        cmd.Parameters.AddWithValue("@nr", stock.NetRate);
        cmd.Parameters.AddWithValue("@mrp", stock.MRP);
        cmd.Parameters.AddWithValue("@sr", stock.SaleRate);
        cmd.Parameters.AddWithValue("@opq", stock.OpeningQty);
        cmd.Parameters.AddWithValue("@purq", stock.PurchasedQty);
        cmd.Parameters.AddWithValue("@salq", stock.SoldQty);
        cmd.Parameters.AddWithValue("@cnq", stock.CreditNoteQty);
        cmd.Parameters.AddWithValue("@stiq", stock.StockInQty);
        cmd.Parameters.AddWithValue("@stoq", stock.StockOutQty);
        cmd.Parameters.AddWithValue("@key", stock.StockKey);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<bool> UpdateAsync(Stock stock)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Stocks SET
            ActualRate=@ar,NetRate=@nr,MRP=@mrp,SaleRate=@sr,
            OpeningQty=@opq,PurchasedQty=@purq,SoldQty=@salq,CreditNoteQty=@cnq,
            StockInQty=@stiq,StockOutQty=@stoq,UpdatedAt=datetime('now')
            WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@ar", stock.ActualRate);
        cmd.Parameters.AddWithValue("@nr", stock.NetRate);
        cmd.Parameters.AddWithValue("@mrp", stock.MRP);
        cmd.Parameters.AddWithValue("@sr", stock.SaleRate);
        cmd.Parameters.AddWithValue("@opq", stock.OpeningQty);
        cmd.Parameters.AddWithValue("@purq", stock.PurchasedQty);
        cmd.Parameters.AddWithValue("@salq", stock.SoldQty);
        cmd.Parameters.AddWithValue("@cnq", stock.CreditNoteQty);
        cmd.Parameters.AddWithValue("@stiq", stock.StockInQty);
        cmd.Parameters.AddWithValue("@stoq", stock.StockOutQty);
        cmd.Parameters.AddWithValue("@id", stock.Id);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeductSoldQtyAsync(int stockId, decimal qty)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Stocks SET SoldQty=SoldQty+@qty,UpdatedAt=datetime('now')
                            WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@qty", qty);
        cmd.Parameters.AddWithValue("@id", stockId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> AddCreditNoteQtyAsync(int stockId, decimal qty)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Stocks SET CreditNoteQty=CreditNoteQty+@qty,UpdatedAt=datetime('now')
                            WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@qty", qty);
        cmd.Parameters.AddWithValue("@id", stockId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeductStockOutQtyAsync(int stockId, decimal qty)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Stocks SET StockOutQty=StockOutQty+@qty,UpdatedAt=datetime('now')
                            WHERE Id=@id AND TenantId=@tid";
        cmd.Parameters.AddWithValue("@qty", qty);
        cmd.Parameters.AddWithValue("@id", stockId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<decimal> GetCurrentQtyAsync(int productId)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT COALESCE(SUM(OpeningQty+PurchasedQty+CreditNoteQty+StockInQty-SoldQty-StockOutQty),0)
            FROM Stocks WHERE ProductId=@pid AND TenantId=@tid AND IsDeleted=0";
        cmd.Parameters.AddWithValue("@pid", productId);
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
    }

    public async Task<decimal> GetTotalStockValueAsync()
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT COALESCE(SUM((OpeningQty+PurchasedQty+CreditNoteQty+StockInQty-SoldQty-StockOutQty)*ActualRate),0)
            FROM Stocks WHERE TenantId=@tid AND IsDeleted=0";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        return Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
    }

    private static Stock MapStock(SqliteDataReader r)
    {
        var s = new Stock
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            TenantId = r.GetInt32(r.GetOrdinal("TenantId")),
            ProductId = r.GetInt32(r.GetOrdinal("ProductId")),
            BatchNo = r.GetString(r.GetOrdinal("BatchNo")),
            ExpiryMY = r.GetString(r.GetOrdinal("ExpiryMY")),
            ExpiryDate = r.GetString(r.GetOrdinal("ExpiryDate")),
            GodownCode = r.GetString(r.GetOrdinal("GodownCode")),
            ActualRate = r.GetDecimal(r.GetOrdinal("ActualRate")),
            NetRate = r.GetDecimal(r.GetOrdinal("NetRate")),
            MRP = r.GetDecimal(r.GetOrdinal("MRP")),
            SaleRate = r.GetDecimal(r.GetOrdinal("SaleRate")),
            OpeningQty = r.GetDecimal(r.GetOrdinal("OpeningQty")),
            PurchasedQty = r.GetDecimal(r.GetOrdinal("PurchasedQty")),
            SoldQty = r.GetDecimal(r.GetOrdinal("SoldQty")),
            CreditNoteQty = r.GetDecimal(r.GetOrdinal("CreditNoteQty")),
            StockInQty = r.GetDecimal(r.GetOrdinal("StockInQty")),
            StockOutQty = r.GetDecimal(r.GetOrdinal("StockOutQty")),
            StockKey = r.GetString(r.GetOrdinal("StockKey")),
            IsDeleted = r.GetInt32(r.GetOrdinal("IsDeleted")) == 1,
            ProductName = SafeStr(r, "ProductName"),
            ProductCode = SafeStr(r, "ProductCode"),
            HSNCode = SafeStr(r, "HSNCode"),
            ManufacturerName = SafeStr(r, "ManufacturerName"),
            CategoryName = SafeStr(r, "CategoryName"),
        };
        return s;
    }

    private static string? SafeStr(SqliteDataReader r, string col)
    {
        try { var i = r.GetOrdinal(col); return r.IsDBNull(i) ? null : r.GetString(i); }
        catch { return null; }
    }
}
