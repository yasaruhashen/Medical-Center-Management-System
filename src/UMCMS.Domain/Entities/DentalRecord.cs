namespace UMCMS.Domain.Entities
{
    /// <summary>Dental-specific record: per-tooth chart (JSON), gum health and X-ray notes.</summary>
    public class DentalRecord
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? AppointmentId { get; set; }

        /// <summary>JSON map of tooth-number → status (e.g. {"11":"Filled","12":"Healthy"}).</summary>
        public string ToothChartJson { get; set; } = "{}";
        public string GumHealth { get; set; } = string.Empty;
        public string XrayNotes { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime RecordDate { get; set; } = DateTime.Now;
    }
}
