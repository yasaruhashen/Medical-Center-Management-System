using System.Data;
using UMCMS.Domain.Entities;

namespace UMCMS.Data.Repositories
{
    public interface IMedicalRecordRepository
    {
        DataTable GetViewByPatient(int patientId);
        int Add(MedicalRecord r);
        int AddPrescription(Prescription p);
        DataTable GetPrescriptions(int medicalRecordId);
        DataTable GetPrescriptionsByPatient(int patientId);
        DataTable GetRecordDetail(int medicalRecordId);

        // Pharmacist dispensary
        DataTable GetPendingConsultations();
        DataTable GetPendingPrescriptions(int medicalRecordId);
        List<Prescription> GetPendingPrescriptionEntities(int medicalRecordId);
        void MarkDispensed(int prescriptionId);
    }

    public class MedicalRecordRepository : IMedicalRecordRepository
    {
        private readonly DatabaseService _db;
        public MedicalRecordRepository(DatabaseService db) => _db = db;

        public DataTable GetViewByPatient(int patientId) => _db.ExecuteQuery(
            @"SELECT m.Id, m.RecordDate AS [Date], u.FirstName||' '||u.LastName AS Doctor,
                     m.Diagnosis, m.Treatment, m.BloodPressure AS BP, m.Pulse, m.Temperature AS Temp, m.Weight
              FROM MedicalRecords m JOIN Users u ON u.Id=m.DoctorId
              WHERE m.PatientId=@p ORDER BY m.RecordDate DESC;",
            new Dictionary<string, object?> { ["p"] = patientId });

        public int Add(MedicalRecord r) => (int)_db.ExecuteInsertReturnId(
            @"INSERT INTO MedicalRecords (PatientId,DoctorId,AppointmentId,Symptoms,Diagnosis,Treatment,BloodPressure,Pulse,Temperature,Weight,Notes,RecordDate)
              VALUES (@p,@d,@a,@sy,@dg,@tr,@bp,@pu,@te,@we,@no,@rd);",
            new Dictionary<string, object?>
            {
                ["p"] = r.PatientId, ["d"] = r.DoctorId, ["a"] = r.AppointmentId,
                ["sy"] = r.Symptoms, ["dg"] = r.Diagnosis, ["tr"] = r.Treatment,
                ["bp"] = r.BloodPressure, ["pu"] = r.Pulse, ["te"] = r.Temperature, ["we"] = r.Weight,
                ["no"] = r.Notes, ["rd"] = r.RecordDate.ToString("yyyy-MM-dd HH:mm:ss")
            });

        public int AddPrescription(Prescription p) => (int)_db.ExecuteInsertReturnId(
            @"INSERT INTO Prescriptions (MedicalRecordId,InventoryItemId,Medicine,Dosage,Instructions,Quantity,TimesPerDay,MealTiming,Dispensed,CreatedAt)
              VALUES (@m,@i,@med,@dos,@ins,@q,@tpd,@mt,@disp,@c);",
            new Dictionary<string, object?>
            {
                ["m"] = p.MedicalRecordId, ["i"] = p.InventoryItemId, ["med"] = p.Medicine,
                ["dos"] = p.Dosage, ["ins"] = p.Instructions, ["q"] = p.Quantity,
                ["tpd"] = p.TimesPerDay, ["mt"] = p.MealTiming,
                ["disp"] = p.Dispensed ? 1 : 0, ["c"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

        public DataTable GetPrescriptions(int medicalRecordId) => _db.ExecuteQuery(
            @"SELECT Medicine, Quantity AS Qty, TimesPerDay AS [Times/day], MealTiming AS Timing,
                     CASE Dispensed WHEN 1 THEN 'Yes' ELSE 'No' END AS Dispensed
              FROM Prescriptions WHERE MedicalRecordId=@m ORDER BY Id;",
            new Dictionary<string, object?> { ["m"] = medicalRecordId });

        public DataTable GetRecordDetail(int medicalRecordId) => _db.ExecuteQuery(
            @"SELECT m.RecordDate, u.FirstName||' '||u.LastName AS Doctor,
                     m.Symptoms, m.Diagnosis, m.Treatment,
                     m.BloodPressure, m.Pulse, m.Temperature, m.Weight, m.Notes
              FROM MedicalRecords m JOIN Users u ON u.Id=m.DoctorId WHERE m.Id=@id;",
            new Dictionary<string, object?> { ["id"] = medicalRecordId });

        public DataTable GetPrescriptionsByPatient(int patientId) => _db.ExecuteQuery(
            @"SELECT m.RecordDate AS [Date], rx.Medicine, rx.Quantity AS Qty,
                     rx.TimesPerDay AS [Times/day], rx.MealTiming AS Timing,
                     u.FirstName||' '||u.LastName AS [Prescribed by],
                     CASE rx.Dispensed WHEN 1 THEN 'Yes' ELSE 'Pending' END AS Dispensed
              FROM Prescriptions rx
              JOIN MedicalRecords m ON m.Id=rx.MedicalRecordId
              JOIN Users u ON u.Id=m.DoctorId
              WHERE m.PatientId=@p ORDER BY m.RecordDate DESC, rx.Id;",
            new Dictionary<string, object?> { ["p"] = patientId });

        // ── Pharmacist dispensary ──
        public DataTable GetPendingConsultations() => _db.ExecuteQuery(
            @"SELECT m.Id,
                     COALESCE(p.RegistrationNumber,'—') AS [Uni ID],
                     p.FirstName||' '||p.LastName AS Patient,
                     u.FirstName||' '||u.LastName AS Doctor,
                     COUNT(rx.Id) AS Medicines,
                     m.RecordDate AS [Prescribed]
              FROM MedicalRecords m
              JOIN Patients p ON p.Id=m.PatientId
              JOIN Users u ON u.Id=m.DoctorId
              JOIN Prescriptions rx ON rx.MedicalRecordId=m.Id AND rx.Dispensed=0
              GROUP BY m.Id ORDER BY m.RecordDate DESC;");

        public DataTable GetPendingPrescriptions(int medicalRecordId) => _db.ExecuteQuery(
            @"SELECT Medicine, Quantity AS Qty, TimesPerDay AS [Times/day], MealTiming AS Timing,
                     CASE WHEN InventoryItemId IS NULL THEN 'Not linked' ELSE 'In stock' END AS Source
              FROM Prescriptions WHERE MedicalRecordId=@m AND Dispensed=0 ORDER BY Id;",
            new Dictionary<string, object?> { ["m"] = medicalRecordId });

        public List<Prescription> GetPendingPrescriptionEntities(int medicalRecordId) => _db.Query(
            "SELECT Id, InventoryItemId, Medicine, Quantity FROM Prescriptions WHERE MedicalRecordId=@m AND Dispensed=0;",
            r => new Prescription
            {
                Id = Convert.ToInt32(r["Id"]),
                InventoryItemId = r["InventoryItemId"] is DBNull ? null : Convert.ToInt32(r["InventoryItemId"]),
                Medicine = r["Medicine"] as string ?? "",
                Quantity = Convert.ToInt32(r["Quantity"])
            },
            new Dictionary<string, object?> { ["m"] = medicalRecordId });

        public void MarkDispensed(int prescriptionId) =>
            _db.ExecuteNonQuery("UPDATE Prescriptions SET Dispensed=1 WHERE Id=@id;",
                new Dictionary<string, object?> { ["id"] = prescriptionId });
    }
}
