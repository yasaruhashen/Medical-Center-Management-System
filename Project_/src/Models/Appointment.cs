namespace Project_.src.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled
        public string Notes { get; set; } = string.Empty;
    }
}
