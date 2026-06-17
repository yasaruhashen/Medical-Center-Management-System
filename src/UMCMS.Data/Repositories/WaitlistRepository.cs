using System.Data;
using UMCMS.Domain.Entities;

namespace UMCMS.Data.Repositories
{
    public interface IWaitlistRepository
    {
        DataTable GetView(string? stream);
        int Add(WaitlistEntry e);
        void SetStatus(int id, string status);
        void Offer(int id);
        void Remove(int id);
        string? GetStream(int id);
        /// <summary>Top still-waiting entry for a stream (id, patient name, phone) or null.</summary>
        (int Id, string Name, string Phone)? NextWaiting(string stream);
        /// <summary>Offered entries whose acceptance window has elapsed.</summary>
        List<(int Id, string Stream)> StaleOffers(int minutes);
    }

    public class WaitlistRepository : IWaitlistRepository
    {
        private readonly DatabaseService _db;
        public WaitlistRepository(DatabaseService db) => _db = db;

        public DataTable GetView(string? stream)
        {
            var sql = @"SELECT w.Id, p.FirstName||' '||p.LastName AS Patient, w.Stream,
                               w.Priority, w.Status, w.CreatedAt AS [Added], w.OfferedAt AS [Offered at]
                        FROM WaitlistEntries w JOIN Patients p ON p.Id=w.PatientId
                        WHERE w.Status IN ('Waiting','Offered')";
            var p = new Dictionary<string, object?>();
            if (stream is not null) { sql += " AND w.Stream=@s"; p["s"] = stream; }
            // Offered first, then by priority/age.
            sql += " ORDER BY CASE w.Status WHEN 'Offered' THEN 0 ELSE 1 END, w.Priority, w.CreatedAt;";
            return _db.ExecuteQuery(sql, p);
        }

        public int Add(WaitlistEntry e) => (int)_db.ExecuteInsertReturnId(
            @"INSERT INTO WaitlistEntries (PatientId,Stream,Priority,Status,CreatedAt)
              VALUES (@p,@s,@pr,'Waiting',@c);",
            new Dictionary<string, object?>
            {
                ["p"] = e.PatientId, ["s"] = e.Stream, ["pr"] = e.Priority,
                ["c"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

        public void SetStatus(int id, string status) =>
            _db.ExecuteNonQuery("UPDATE WaitlistEntries SET Status=@s WHERE Id=@id;",
                new Dictionary<string, object?> { ["s"] = status, ["id"] = id });

        public void Offer(int id) =>
            _db.ExecuteNonQuery("UPDATE WaitlistEntries SET Status='Offered', OfferedAt=@t WHERE Id=@id;",
                new Dictionary<string, object?> { ["t"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ["id"] = id });

        public void Remove(int id) =>
            _db.ExecuteNonQuery("DELETE FROM WaitlistEntries WHERE Id=@id;",
                new Dictionary<string, object?> { ["id"] = id });

        public string? GetStream(int id) => _db.ExecuteScalar<string>(
            "SELECT Stream FROM WaitlistEntries WHERE Id=@id;", new Dictionary<string, object?> { ["id"] = id });

        public (int Id, string Name, string Phone)? NextWaiting(string stream)
        {
            var r = _db.Query(
                @"SELECT w.Id AS Id, p.FirstName||' '||p.LastName AS Name, p.PhoneNumber AS Phone
                  FROM WaitlistEntries w JOIN Patients p ON p.Id=w.PatientId
                  WHERE w.Stream=@s AND w.Status='Waiting'
                  ORDER BY w.Priority, w.CreatedAt LIMIT 1;",
                r => (Id: Convert.ToInt32(r["Id"]), Name: r["Name"] as string ?? "", Phone: r["Phone"] as string ?? ""),
                new Dictionary<string, object?> { ["s"] = stream }).FirstOrDefault();
            return r.Id == 0 ? null : r;
        }

        public List<(int Id, string Stream)> StaleOffers(int minutes) => _db.Query(
            @"SELECT Id, Stream FROM WaitlistEntries
              WHERE Status='Offered' AND OfferedAt IS NOT NULL
                AND OfferedAt <= @cut;",
            r => (Convert.ToInt32(r["Id"]), r["Stream"] as string ?? "General"),
            new Dictionary<string, object?> { ["cut"] = DateTime.Now.AddMinutes(-minutes).ToString("yyyy-MM-dd HH:mm:ss") });
    }
}
