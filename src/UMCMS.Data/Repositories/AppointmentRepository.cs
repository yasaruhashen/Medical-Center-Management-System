using System.Data;
using UMCMS.Domain.Entities;

namespace UMCMS.Data.Repositories
{
    public interface IAppointmentRepository
    {
        DataTable GetView(DateTime? day, int? doctorId);
        int Add(Appointment a);
        void SetStatus(int id, string status);
        void Reschedule(int id, DateTime start);
        bool HasCollision(int doctorId, DateTime start, int durationMinutes, int? excludeId = null);
        Appointment? GetById(int id);
    }

    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly DatabaseService _db;
        public AppointmentRepository(DatabaseService db) => _db = db;

        /// <summary>Display rows joined with patient and doctor names (optionally filtered).</summary>
        public DataTable GetView(DateTime? day, int? doctorId)
        {
            var sql = @"SELECT a.Id, a.PatientId,
                               a.AppointmentDate AS [When],
                               p.FirstName||' '||p.LastName AS Patient,
                               u.FirstName||' '||u.LastName AS Doctor,
                               a.Stream, a.Status, a.Notes
                        FROM Appointments a
                        JOIN Patients p ON p.Id=a.PatientId
                        JOIN Users u ON u.Id=a.DoctorId
                        WHERE 1=1";
            var p = new Dictionary<string, object?>();
            if (day is not null) { sql += " AND date(a.AppointmentDate)=@d"; p["d"] = day.Value.ToString("yyyy-MM-dd"); }
            if (doctorId is not null) { sql += " AND a.DoctorId=@doc"; p["doc"] = doctorId.Value; }
            sql += " ORDER BY a.AppointmentDate;";
            return _db.ExecuteQuery(sql, p);
        }

        public Appointment? GetById(int id) => _db.Query(
            "SELECT * FROM Appointments WHERE Id=@id;",
            r => new Appointment
            {
                Id = Convert.ToInt32(r["Id"]),
                PatientId = Convert.ToInt32(r["PatientId"]),
                DoctorId = Convert.ToInt32(r["DoctorId"]),
                AppointmentDate = DateTime.Parse((string)r["AppointmentDate"]),
                DurationMinutes = Convert.ToInt32(r["DurationMinutes"]),
                Status = (string)r["Status"],
                Stream = (string)r["Stream"],
                Notes = r["Notes"] as string ?? ""
            },
            new Dictionary<string, object?> { ["id"] = id }).FirstOrDefault();

        public int Add(Appointment a) => (int)_db.ExecuteInsertReturnId(
            @"INSERT INTO Appointments (PatientId,DoctorId,AppointmentDate,DurationMinutes,Status,Stream,Notes,CreatedAt)
              VALUES (@p,@d,@dt,@dur,@st,@str,@n,@c);",
            new Dictionary<string, object?>
            {
                ["p"] = a.PatientId, ["d"] = a.DoctorId,
                ["dt"] = a.AppointmentDate.ToString("yyyy-MM-dd HH:mm:ss"),
                ["dur"] = a.DurationMinutes, ["st"] = a.Status, ["str"] = a.Stream,
                ["n"] = a.Notes, ["c"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

        public void SetStatus(int id, string status) =>
            _db.ExecuteNonQuery("UPDATE Appointments SET Status=@s WHERE Id=@id;",
                new Dictionary<string, object?> { ["s"] = status, ["id"] = id });

        public void Reschedule(int id, DateTime start) =>
            _db.ExecuteNonQuery("UPDATE Appointments SET AppointmentDate=@dt, Status='Scheduled' WHERE Id=@id;",
                new Dictionary<string, object?> { ["dt"] = start.ToString("yyyy-MM-dd HH:mm:ss"), ["id"] = id });

        /// <summary>True if the doctor already has an overlapping non-cancelled appointment.</summary>
        public bool HasCollision(int doctorId, DateTime start, int durationMinutes, int? excludeId = null)
        {
            var end = start.AddMinutes(durationMinutes);
            var sql = @"SELECT COUNT(*) FROM Appointments
                        WHERE DoctorId=@doc AND Status NOT IN ('Cancelled','NoShow')
                          AND @start < datetime(AppointmentDate, '+' || DurationMinutes || ' minutes')
                          AND @end > AppointmentDate";
            var p = new Dictionary<string, object?>
            {
                ["doc"] = doctorId,
                ["start"] = start.ToString("yyyy-MM-dd HH:mm:ss"),
                ["end"] = end.ToString("yyyy-MM-dd HH:mm:ss")
            };
            if (excludeId is not null) { sql += " AND Id<>@ex"; p["ex"] = excludeId.Value; }
            return _db.ExecuteScalar<long>(sql + ";", p) > 0;
        }
    }
}
