using System.IO;
using UMCMS.Data;
using UMCMS.Data.Repositories;
using UMCMS.Domain.Entities;
using UMCMS.Services;
using Xunit;

namespace UMCMS.Tests
{
    public class WaitlistAutoFillTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly DatabaseService _db;
        private readonly AppointmentRepository _appts;
        private readonly WaitlistRepository _waitlist;
        private readonly AppointmentService _service;

        private readonly int _genDoctor, _dentist, _p1, _p2, _p3;

        public WaitlistAutoFillTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "umcms_wl_" + Guid.NewGuid().ToString("N") + ".db");
            _db = new DatabaseService(_dbPath);
            _db.InitializeDatabase(seedDemoData: false);   // clean DB; build our own fixture

            var users = new UserRepository(_db);
            var patients = new PatientRepository(_db);
            _appts = new AppointmentRepository(_db);
            _waitlist = new WaitlistRepository(_db);
            _service = new AppointmentService(_appts, _waitlist, _db, new NotificationService(_db), new AuditService(_db));

            _genDoctor = users.Add(new User { FirstName = "Gen", LastName = "Doc", Username = "g", PasswordHash = "x", Role = "Doctor", Specialization = "General" });
            _dentist = users.Add(new User { FirstName = "Den", LastName = "Tist", Username = "d", PasswordHash = "x", Role = "Doctor", Specialization = "Dental" });
            _p1 = patients.Add(new Patient { FirstName = "Pat", LastName = "One", PatientType = "External", PhoneNumber = "0711111111" });
            _p2 = patients.Add(new Patient { FirstName = "Pat", LastName = "Two", PatientType = "External", PhoneNumber = "0722222222" });
            _p3 = patients.Add(new Patient { FirstName = "Pat", LastName = "Three", PatientType = "External", PhoneNumber = "0733333333" });
        }

        [Fact]
        public void Cancel_OffersSlotToTopWaitlistedPatient_AndLogsNotification()
        {
            _waitlist.Add(new WaitlistEntry { PatientId = _p2, Stream = "General", Priority = 3 });
            int topId = _waitlist.Add(new WaitlistEntry { PatientId = _p1, Stream = "General", Priority = 1 }); // highest priority

            int apptId = _appts.Add(new Appointment
            {
                PatientId = _p3, DoctorId = _genDoctor, AppointmentDate = DateTime.Today.AddDays(1).AddHours(9),
                DurationMinutes = 30, Stream = "General", Status = "Scheduled"
            });

            long notifBefore = _db.ExecuteScalar<long>("SELECT COUNT(*) FROM NotificationLogs;");
            string message = _service.CancelAndBackfill(apptId);

            Assert.Contains("offered", message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("Cancelled", Status("Appointments", apptId));
            Assert.Equal("Offered", Status("WaitlistEntries", topId));   // priority-1 entry chosen
            Assert.Equal(notifBefore + 1, _db.ExecuteScalar<long>("SELECT COUNT(*) FROM NotificationLogs;"));
        }

        [Fact]
        public void Cancel_WithNoWaitlist_StillCancels_NoOffer()
        {
            int apptId = _appts.Add(new Appointment
            {
                PatientId = _p1, DoctorId = _dentist, AppointmentDate = DateTime.Today.AddDays(1).AddHours(10),
                DurationMinutes = 30, Stream = "Dental", Status = "Scheduled"
            });

            string message = _service.CancelAndBackfill(apptId);

            Assert.Contains("No one", message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("Cancelled", Status("Appointments", apptId));
        }

        [Fact]
        public void Collision_DetectedForOverlappingSameDoctor()
        {
            var start = DateTime.Today.AddDays(2).AddHours(9);
            _appts.Add(new Appointment { PatientId = _p1, DoctorId = _genDoctor, AppointmentDate = start, DurationMinutes = 30, Stream = "General", Status = "Scheduled" });

            Assert.True(_appts.HasCollision(_genDoctor, start.AddMinutes(15), 30));   // overlaps
            Assert.False(_appts.HasCollision(_genDoctor, start.AddHours(2), 30));     // clear
            Assert.False(_appts.HasCollision(_dentist, start.AddMinutes(15), 30));    // different doctor
        }

        private string? Status(string table, int id) =>
            _db.ExecuteScalar<string>($"SELECT Status FROM {table} WHERE Id=@i;",
                new Dictionary<string, object?> { ["i"] = id });

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { File.Delete(_dbPath); } catch { }
        }
    }
}
