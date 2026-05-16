using Medital_Application.Models;
using Medital_Application.Responses;

namespace Medital_Application.Services.Interfaces;

public interface IStockService
{
    Task<List<StockResponse>> GetCurrentStockAsync(string? search = null, int? categoryId = null, int? manufacturerId = null);
    Task<List<StockResponse>> GetExpiringStockAsync(int monthsAhead = 3);
    Task<List<StockResponse>> GetExpiredStockAsync();
    Task<List<StockResponse>> GetLowStockAsync();
    Task<decimal> GetCurrentQtyAsync(int productId);
    Task<List<Stock>> GetBatchesAsync(int productId);
    Task<decimal> GetTotalStockValueAsync();
}
