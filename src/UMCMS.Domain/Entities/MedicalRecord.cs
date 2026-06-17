namespace UMCMS.Domain.Entities
{
    public class MedicalRecord
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? AppointmentId { get; set; }

        public string Symptoms { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string Treatment { get; set; } = string.Empty;

        // Vitals
        public string? BloodPressure { get; set; }
        public int? Pulse { get; set; }
        public double? Temperature { get; set; }
        public double? Weight { get; set; }

        public string Notes { get; set; } = string.Empty;
        public DateTime RecordDate { get; set; } = DateTime.Now;
    }
}
