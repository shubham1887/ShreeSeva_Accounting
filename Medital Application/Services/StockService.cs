using Medital_Application.Models;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Responses;
using Medital_Application.Services.Interfaces;

namespace Medital_Application.Services;

public class StockService : IStockService
{
    private readonly IStockRepository _stockRepo;

    public StockService(IStockRepository stockRepo) => _stockRepo = stockRepo;

    public async Task<List<StockResponse>> GetCurrentStockAsync(string? search = null, int? categoryId = null, int? manufacturerId = null)
    {
        var stocks = await _stockRepo.GetAllAsync(search, categoryId, manufacturerId);
        return stocks.Select(MapResponse).ToList();
    }

    public async Task<List<StockResponse>> GetExpiringStockAsync(int monthsAhead = 3)
    {
        var stocks = await _stockRepo.GetExpiringAsync(monthsAhead);
        return stocks.Select(s => { var r = MapResponse(s); r.ExpiryStatus = "EXPIRING_SOON"; return r; }).ToList();
    }

    public async Task<List<StockResponse>> GetExpiredStockAsync()
    {
        var stocks = await _stockRepo.GetExpiredAsync();
        return stocks.Select(s => { var r = MapResponse(s); r.ExpiryStatus = "EXPIRED"; return r; }).ToList();
    }

    public async Task<List<StockResponse>> GetLowStockAsync()
    {
        var stocks = await _stockRepo.GetLowStockAsync();
        return stocks.Select(s => { var r = MapResponse(s); r.IsLowStock = true; return r; }).ToList();
    }

    public Task<decimal> GetCurrentQtyAsync(int productId) => _stockRepo.GetCurrentQtyAsync(productId);

    public Task<List<Stock>> GetBatchesAsync(int productId) => _stockRepo.GetAvailableBatchesAsync(productId);

    public Task<decimal> GetTotalStockValueAsync() => _stockRepo.GetTotalStockValueAsync();

    private static StockResponse MapResponse(Stock s)
    {
        DateTime? expDate = DateTime.TryParse(s.ExpiryDate, out var d) ? d : null;
        var status = expDate.HasValue
            ? expDate.Value < DateTime.Today ? "EXPIRED"
              : expDate.Value <= DateTime.Today.AddMonths(3) ? "EXPIRING_SOON"
              : "GOOD"
            : "GOOD";

        return new StockResponse
        {
            StockId = s.Id,
            ProductId = s.ProductId,
            ProductName = s.ProductName ?? "",
            ProductCode = s.ProductCode ?? "",
            ManufacturerName = s.ManufacturerName ?? "",
            CategoryName = s.CategoryName ?? "",
            BatchNo = s.BatchNo,
            ExpiryMY = s.ExpiryMY,
            ExpiryDate = expDate,
            CurrentQty = s.CurrentQty,
            ActualRate = s.ActualRate,
            SaleRate = s.SaleRate,
            MRP = s.MRP,
            ExpiryStatus = status,
            HSNCode = s.HSNCode,
        };
    }
}
