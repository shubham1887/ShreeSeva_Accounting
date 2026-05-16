using Medital_Application.Models;
using Medital_Application.Requests;

namespace Medital_Application.Repositories.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> SearchAsync(SearchProductRequest request);
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<List<Product>> GetLowStockAsync();
    Task<int> CreateAsync(Product product);
    Task<bool> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
    Task<bool> UpdateStockQtyAsync(int productId, decimal qty);
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByCodeAsync(string code);
}
