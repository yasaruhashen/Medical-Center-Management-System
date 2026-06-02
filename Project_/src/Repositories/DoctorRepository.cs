using Project_.src.Models;

namespace Project_.src.Repositories
{
    public class DoctorRepository : IDoctorRepository
    {
        public Task<Doctor?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Doctor>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(Doctor doctor)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Doctor doctor)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
