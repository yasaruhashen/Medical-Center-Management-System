using Project_.src.Models;

namespace Project_.src.Repositories
{
    public interface IPatientRepository
    {
        Task<Patient?> GetByIdAsync(int id);
        Task<IEnumerable<Patient>> GetAllAsync();
        Task AddAsync(Patient patient);
        Task UpdateAsync(Patient patient);
        Task DeleteAsync(int id);
    }
}
