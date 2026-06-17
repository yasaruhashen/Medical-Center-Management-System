using UMCMS.Data;

namespace UMCMS.Services
{
    /// <summary>
    /// Centralized, parameterized notification templates (FR-NOTI-4).
    /// </summary>
    public static class NotificationTemplates
    {
        public static string WaitlistOffer(string stream, DateTime when) =>
            $"A {stream} appointment slot on {when:ddd dd MMM HH:mm} is now available. Please contact the University Medical Centre to confirm.";

        public static string DoctorOnSite(string doctorLastName) =>
            $"Dr. {doctorLastName} is now on-site at the University Medical Centre. You may visit.";

        public static string AppointmentReminder(string doctorName, DateTime when) =>
            $"Reminder: you have an appointment with {doctorName} on {when:ddd dd MMM HH:mm}.";

        public static string Emergency(string patientName, string detail) =>
            $"EMERGENCY — University Medical Centre regarding {patientName}: {detail} "
            + "Please contact the medical centre immediately on 011-2758000.";
    }

    /// <summary>
    /// Mock notification service: persists every message to NotificationLogs (and the
    /// console) but never sends externally. Each message is recorded as Pending, then
    /// updated to Sent/Failed after a bounded retry with backoff (FR-NOTI-1/3).
    /// </summary>
    public class NotificationService
    {
        private readonly DatabaseService _db;
        public int MaxAttempts { get; init; } = 3;

        public NotificationService(DatabaseService db) => _db = db;

        public void Send(string channel, string recipient, string body)
        {
            long id = _db.ExecuteInsertReturnId(
                @"INSERT INTO NotificationLogs (Channel, Recipient, Body, Status, CreatedAt)
                  VALUES (@c, @r, @b, 'Pending', @t);",
                new Dictionary<string, object?>
                {
                    ["c"] = channel, ["r"] = recipient, ["b"] = body,
                    ["t"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });

            bool delivered = TryDeliver(channel, recipient, body);

            _db.ExecuteNonQuery("UPDATE NotificationLogs SET Status=@s WHERE Id=@id;",
                new Dictionary<string, object?> { ["s"] = delivered ? "Sent" : "Failed", ["id"] = id });
        }

        /// <summary>Attempts delivery with bounded retries and exponential-ish backoff.</summary>
        private bool TryDeliver(string channel, string recipient, string body)
        {
            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    Deliver(channel, recipient, body);   // mock transport
                    return true;
                }
                catch
                {
                    if (attempt == MaxAttempts) return false;
                    Thread.Sleep(40 * attempt);          // backoff
                }
            }
            return false;
        }

        protected virtual void Deliver(string channel, string recipient, string body)
            => Console.WriteLine($"[NOTIFY/{channel}] {recipient}: {body}");
    }
}
