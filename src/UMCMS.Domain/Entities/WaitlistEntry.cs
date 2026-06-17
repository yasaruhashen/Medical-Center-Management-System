namespace UMCMS.Domain.Entities
{
    /// <summary>A student waiting for a slot in a given stream; back-filled on cancellation.</summary>
    public class WaitlistEntry
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string Stream { get; set; } = "General";   // "General" or "Dental"
        public int Priority { get; set; }                  // lower = higher priority
        public string Status { get; set; } = "Waiting";    // Waiting, Offered, Booked, Expired
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
