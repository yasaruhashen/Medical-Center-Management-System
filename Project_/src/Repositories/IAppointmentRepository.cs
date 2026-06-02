using Project_.src.Models;

namespace Project_.src.Repositories
{
    public interface IAppointmentRepository
    {
        Task<Appointment?> GetByIdAsync(int id);
        Task<IEnumerable<Appointment>> GetAllAsync();
        Task<IEnumerable<Appointment>> GetByDoctorIdAsync(int doctorId);
        Task<IEnumerable<Appointment>> GetByPatientIdAsync(int patientId);
        Task AddAsync(Appointment appointment);
        Task UpdateAsync(Appointment appointment);
        Task DeleteAsync(int id);
    }
}
