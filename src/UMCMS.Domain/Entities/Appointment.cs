namespace UMCMS.Domain.Entities
{
    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int DurationMinutes { get; set; } = 30;

        /// <summary>Scheduled, CheckedIn, Completed, Cancelled, NoShow.</summary>
        public string Status { get; set; } = "Scheduled";

        /// <summary>"General" or "Dental" — which clinical stream the visit belongs to.</summary>
        public string Stream { get; set; } = "General";

        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
