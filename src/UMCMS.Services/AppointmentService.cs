using UMCMS.Data;
using UMCMS.Data.Repositories;

namespace UMCMS.Services
{
    /// <summary>
    /// Appointment workflow including the smart-queue: cancelling an appointment offers the
    /// freed slot to the next waitlisted patient (priority, then FIFO) with a notification.
    /// Offers have an acceptance window — if it lapses or the patient declines, the offer
    /// cascades to the next eligible patient.
    /// </summary>
    public class AppointmentService
    {
        /// <summary>Minutes a waitlist offer stays valid before it auto-cascades.</summary>
        public const int OfferWindowMinutes = 30;

        private readonly IAppointmentRepository _appts;
        private readonly IWaitlistRepository _waitlist;
        private readonly DatabaseService _db;
        private readonly NotificationService _notify;
        private readonly AuditService _audit;

        public AppointmentService(IAppointmentRepository appts, IWaitlistRepository waitlist,
                                  DatabaseService db, NotificationService notify, AuditService audit)
        {
            _appts = appts;
            _waitlist = waitlist;
            _db = db;
            _notify = notify;
            _audit = audit;
        }

        /// <summary>Cancels an appointment and offers the freed slot to the waitlist.</summary>
        public string CancelAndBackfill(int appointmentId)
        {
            var appt = _appts.GetById(appointmentId);
            if (appt is null) return "Appointment not found.";

            _appts.SetStatus(appointmentId, "Cancelled");
            _audit.Log("Cancel", "Appointment", $"#{appointmentId} ({appt.Stream}, {appt.AppointmentDate:yyyy-MM-dd HH:mm})");

            var name = OfferNext(appt.Stream, appt.AppointmentDate);
            return name is null
                ? "Appointment cancelled. No one is on the waitlist for this stream."
                : $"Appointment cancelled. The slot was offered to {name} (next on the waitlist) and an SMS notification was logged.";
        }

        /// <summary>Patient responds to an offer. Decline cascades the offer to the next in line.</summary>
        public string RespondToOffer(int waitlistId, bool accepted)
        {
            var stream = _waitlist.GetStream(waitlistId) ?? "General";
            if (accepted)
            {
                _waitlist.SetStatus(waitlistId, "Accepted");
                _audit.Log("WaitlistAccept", "Waitlist", $"#{waitlistId} accepted ({stream})");
                return "Offer accepted — please book the confirmed appointment for this patient.";
            }

            _waitlist.SetStatus(waitlistId, "Declined");
            _audit.Log("WaitlistDecline", "Waitlist", $"#{waitlistId} declined ({stream})");
            var name = OfferNext(stream, null);
            return name is null
                ? "Offer declined. No one else is waiting for this stream."
                : $"Offer declined. It was cascaded to {name} (next on the waitlist).";
        }

        /// <summary>
        /// Expires offers older than the acceptance window and cascades each to the next
        /// waiting patient. Returns the number of offers that expired. Call on screen load.
        /// </summary>
        public int ExpireStaleOffers(int minutes = OfferWindowMinutes)
        {
            var stale = _waitlist.StaleOffers(minutes);
            foreach (var (id, stream) in stale)
            {
                _waitlist.SetStatus(id, "Expired");
                _audit.Log("WaitlistExpire", "Waitlist", $"#{id} offer expired ({stream})");
                OfferNext(stream, null);
            }
            return stale.Count;
        }

        /// <summary>Manually offers a specific waitlist entry (receptionist action).</summary>
        public string OfferSpecific(int waitlistId)
        {
            var info = _db.Query(
                @"SELECT w.Stream AS Stream, p.FirstName||' '||p.LastName AS Name, p.PhoneNumber AS Phone
                  FROM WaitlistEntries w JOIN Patients p ON p.Id=w.PatientId WHERE w.Id=@id;",
                r => (Stream: r["Stream"] as string ?? "General", Name: r["Name"] as string ?? "", Phone: r["Phone"] as string ?? ""),
                new Dictionary<string, object?> { ["id"] = waitlistId }).FirstOrDefault();
            if (info.Name is null or "") return "Waitlist entry not found.";

            _waitlist.Offer(waitlistId);
            var recipient = string.IsNullOrWhiteSpace(info.Phone) ? info.Name : info.Phone;
            _notify.Send("SMS", recipient, NotificationTemplates.WaitlistOffer(info.Stream, DateTime.Now));
            _audit.Log("WaitlistOffer", "Waitlist", $"Manually offered {info.Stream} slot to {info.Name}");
            return $"Slot offered to {info.Name}; an SMS notification was logged.";
        }

        /// <summary>Offers the slot to the next waiting patient for a stream; returns their name or null.</summary>
        private string? OfferNext(string stream, DateTime? slot)
        {
            var next = _waitlist.NextWaiting(stream);
            if (next is null) return null;

            _waitlist.Offer(next.Value.Id);
            var recipient = string.IsNullOrWhiteSpace(next.Value.Phone) ? next.Value.Name : next.Value.Phone;
            _notify.Send("SMS", recipient, NotificationTemplates.WaitlistOffer(stream, slot ?? DateTime.Now));
            _audit.Log("WaitlistOffer", "Waitlist", $"Offered {stream} slot to {next.Value.Name}");
            return next.Value.Name;
        }
    }
}
