using Medital_Application.Models;
using Medital_Application.Requests;
using Medital_Application.Responses;

namespace Medital_Application.Services.Interfaces;

public interface IPurchaseService
{
    Task<ApiResponse<PurchaseResponse>> CreatePurchaseAsync(CreatePurchaseRequest request);
    Task<PurchaseMaster?> GetByIdAsync(int id);
    Task<List<PurchaseMaster>> GetByDateRangeAsync(DateTime from, DateTime to, int? accountId = null);
    Task<ApiResponse> CancelPurchaseAsync(int id, int userId);
}
