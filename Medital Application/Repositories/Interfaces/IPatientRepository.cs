using Medital_Application.Models;

namespace Medital_Application.Repositories.Interfaces;

public interface IPatientRepository
{
    Task<List<Patient>> SearchAsync(string? searchTerm, int? doctorId = null);
    Task<Patient?> GetByIdAsync(int id);
    Task<int> CreateAsync(Patient patient);
    Task<bool> UpdateAsync(Patient patient);
    Task<bool> DeleteAsync(int id);
    Task<List<Patient>> GetAllAsync();
}
