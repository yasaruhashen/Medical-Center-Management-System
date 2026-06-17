using System.Data;
using UMCMS.Domain.Entities;

namespace UMCMS.Data.Repositories
{
    public interface IPatientRepository
    {
        List<Patient> GetAll(bool includeArchived = false);
        List<Patient> Search(string term);
        Patient? GetById(int id);
        int Add(Patient p);
        void Update(Patient p);
        void Archive(int id);
    }

    public class PatientRepository : IPatientRepository
    {
        private readonly DatabaseService _db;
        public PatientRepository(DatabaseService db) => _db = db;

        internal static Patient Map(IDataReader r) => new()
        {
            Id = Convert.ToInt32(r["Id"]),
            FirstName = r["FirstName"] as string ?? "",
            LastName = r["LastName"] as string ?? "",
            DateOfBirth = r["DateOfBirth"] is string d && DateTime.TryParse(d, out var dt) ? dt : null,
            Gender = r["Gender"] as string ?? "",
            PhoneNumber = r["PhoneNumber"] as string ?? "",
            Email = r["Email"] as string ?? "",
            Address = r["Address"] as string ?? "",
            PatientType = r["PatientType"] as string ?? "External",
            RegistrationNumber = r["RegistrationNumber"] as string,
            Faculty = r["Faculty"] as string,
            Program = r["Program"] as string,
            YearOfStudy = r["YearOfStudy"] is long y ? (int)y : (r["YearOfStudy"] as int?),
            EmergencyContactName = r["EmergencyContactName"] as string,
            EmergencyContactPhone = r["EmergencyContactPhone"] as string,
            IsArchived = Convert.ToInt64(r["IsArchived"]) == 1
        };

        public List<Patient> GetAll(bool includeArchived = false) =>
            _db.Query($"SELECT * FROM Patients {(includeArchived ? "" : "WHERE IsArchived=0")} ORDER BY LastName, FirstName;", Map);

        public List<Patient> Search(string term) =>
            _db.Query(
                @"SELECT * FROM Patients
                  WHERE IsArchived=0 AND (FirstName LIKE @t OR LastName LIKE @t OR RegistrationNumber LIKE @t OR PhoneNumber LIKE @t)
                  ORDER BY LastName;",
                Map, new Dictionary<string, object?> { ["t"] = "%" + term + "%" });

        public Patient? GetById(int id) =>
            _db.Query("SELECT * FROM Patients WHERE Id=@id;", Map,
                new Dictionary<string, object?> { ["id"] = id }).FirstOrDefault();

        public int Add(Patient p) => (int)_db.ExecuteInsertReturnId(
            @"INSERT INTO Patients (FirstName,LastName,DateOfBirth,Gender,PhoneNumber,Email,Address,PatientType,RegistrationNumber,Faculty,Program,YearOfStudy,EmergencyContactName,EmergencyContactPhone,IsArchived,CreatedAt)
              VALUES (@f,@l,@dob,@g,@ph,@e,@a,@t,@reg,@fac,@prog,@yr,@ecn,@ecp,0,@c);",
            new Dictionary<string, object?>
            {
                ["f"] = p.FirstName, ["l"] = p.LastName,
                ["dob"] = p.DateOfBirth?.ToString("yyyy-MM-dd"), ["g"] = p.Gender,
                ["ph"] = p.PhoneNumber, ["e"] = p.Email, ["a"] = p.Address, ["t"] = p.PatientType,
                ["reg"] = p.RegistrationNumber, ["fac"] = p.Faculty, ["prog"] = p.Program, ["yr"] = p.YearOfStudy,
                ["ecn"] = p.EmergencyContactName, ["ecp"] = p.EmergencyContactPhone,
                ["c"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

        public void Update(Patient p) => _db.ExecuteNonQuery(
            @"UPDATE Patients SET FirstName=@f,LastName=@l,DateOfBirth=@dob,Gender=@g,PhoneNumber=@ph,Email=@e,Address=@a,
                                  PatientType=@t,RegistrationNumber=@reg,Faculty=@fac,Program=@prog,YearOfStudy=@yr,
                                  EmergencyContactName=@ecn,EmergencyContactPhone=@ecp
              WHERE Id=@id;",
            new Dictionary<string, object?>
            {
                ["f"] = p.FirstName, ["l"] = p.LastName, ["dob"] = p.DateOfBirth?.ToString("yyyy-MM-dd"),
                ["g"] = p.Gender, ["ph"] = p.PhoneNumber, ["e"] = p.Email, ["a"] = p.Address, ["t"] = p.PatientType,
                ["reg"] = p.RegistrationNumber, ["fac"] = p.Faculty, ["prog"] = p.Program, ["yr"] = p.YearOfStudy,
                ["ecn"] = p.EmergencyContactName, ["ecp"] = p.EmergencyContactPhone,
                ["id"] = p.Id
            });

        public void Archive(int id) =>
            _db.ExecuteNonQuery("UPDATE Patients SET IsArchived=1 WHERE Id=@id;",
                new Dictionary<string, object?> { ["id"] = id });
    }
}
