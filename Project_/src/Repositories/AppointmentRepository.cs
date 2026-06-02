using Project_.src.Models;

namespace Project_.src.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        public Task<Appointment?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Appointment>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Appointment>> GetByDoctorIdAsync(int doctorId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Appointment>> GetByPatientIdAsync(int patientId)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(Appointment appointment)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Appointment appointment)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
