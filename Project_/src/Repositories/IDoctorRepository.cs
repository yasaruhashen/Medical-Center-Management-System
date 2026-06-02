using Project_.src.Models;

namespace Project_.src.Repositories
{
    public interface IDoctorRepository
    {
        Task<Doctor?> GetByIdAsync(int id);
        Task<IEnumerable<Doctor>> GetAllAsync();
        Task AddAsync(Doctor doctor);
        Task UpdateAsync(Doctor doctor);
        Task DeleteAsync(int id);
    }
}
