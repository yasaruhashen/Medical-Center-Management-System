using Project_.src.Models;

namespace Project_.src.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        public Task<Patient?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Patient>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(Patient patient)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Patient patient)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
