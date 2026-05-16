using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface IStockRepository
{
    Task<List<Stock>> GetByProductAsync(int productId);
    Task<Stock?> GetByKeyAsync(string stockKey);
    Task<Stock?> GetByIdAsync(int id);
    Task<List<Stock>> GetAvailableBatchesAsync(int productId);  // FIFO order, positive qty only
    Task<List<Stock>> GetExpiringAsync(int monthsAhead = 3);
    Task<List<Stock>> GetExpiredAsync();
    Task<List<Stock>> GetLowStockAsync();
    Task<List<Stock>> GetAllAsync(string? searchTerm = null, int? categoryId = null, int? manufacturerId = null);
    Task<int> CreateAsync(Stock stock);
    Task<bool> UpdateAsync(Stock stock);
    Task<bool> DeductSoldQtyAsync(int stockId, decimal qty);
    Task<bool> AddCreditNoteQtyAsync(int stockId, decimal qty);  // return from customer
    Task<bool> DeductStockOutQtyAsync(int stockId, decimal qty); // return to supplier
    Task<decimal> GetCurrentQtyAsync(int productId);
    Task<decimal> GetTotalStockValueAsync();
}
