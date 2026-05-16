using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface IDoctorRepository
{
    Task<List<Doctor>> SearchAsync(string? searchTerm);
    Task<Doctor?> GetByIdAsync(int id);
    Task<int> CreateAsync(Doctor doctor);
    Task<bool> UpdateAsync(Doctor doctor);
    Task<bool> DeleteAsync(int id);
    Task<List<Doctor>> GetAllAsync();
}
