using System.Data;
using UMCMS.Domain.Entities;

namespace UMCMS.Data.Repositories
{
    public interface IDentalRecordRepository
    {
        DataTable GetViewByPatient(int patientId);
        int Add(DentalRecord r);
    }

    public class DentalRecordRepository : IDentalRecordRepository
    {
        private readonly DatabaseService _db;
        public DentalRecordRepository(DatabaseService db) => _db = db;

        public DataTable GetViewByPatient(int patientId) => _db.ExecuteQuery(
            @"SELECT d.Id, d.RecordDate AS [Date], u.FirstName||' '||u.LastName AS Dentist,
                     d.GumHealth AS [Gum health], d.XrayNotes AS [X-ray notes], d.Notes
              FROM DentalRecords d JOIN Users u ON u.Id=d.DoctorId
              WHERE d.PatientId=@p ORDER BY d.RecordDate DESC;",
            new Dictionary<string, object?> { ["p"] = patientId });

        public int Add(DentalRecord r) => (int)_db.ExecuteInsertReturnId(
            @"INSERT INTO DentalRecords (PatientId,DoctorId,AppointmentId,ToothChartJson,GumHealth,XrayNotes,Notes,RecordDate)
              VALUES (@p,@d,@a,@tc,@gh,@xn,@no,@rd);",
            new Dictionary<string, object?>
            {
                ["p"] = r.PatientId, ["d"] = r.DoctorId, ["a"] = r.AppointmentId,
                ["tc"] = r.ToothChartJson, ["gh"] = r.GumHealth, ["xn"] = r.XrayNotes,
                ["no"] = r.Notes, ["rd"] = r.RecordDate.ToString("yyyy-MM-dd HH:mm:ss")
            });
    }
}
