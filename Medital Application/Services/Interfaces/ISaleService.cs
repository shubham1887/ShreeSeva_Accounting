using Medital_Application.Models;
using Medital_Application.Requests;
using Medital_Application.Responses;

namespace Medital_Application.Services.Interfaces;

public interface ISaleService
{
    Task<ApiResponse<SaleResponse>> CreateSaleAsync(CreateSaleRequest request);
    Task<SaleMaster?> GetByIdAsync(int id);
    Task<List<SaleMaster>> GetByDateRangeAsync(DateTime from, DateTime to, int? accountId = null);
    Task<ApiResponse> CancelSaleAsync(int id, int userId);
}
