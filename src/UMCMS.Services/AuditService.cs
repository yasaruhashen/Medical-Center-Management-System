using System.Data;
using UMCMS.Data;

namespace UMCMS.Services
{
    /// <summary>
    /// Records an immutable audit trail of security-relevant and clinical actions.
    /// The acting user is taken from <see cref="Session.CurrentUser"/>.
    /// </summary>
    public class AuditService
    {
        private readonly DatabaseService _db;
        public AuditService(DatabaseService db) => _db = db;

        public void Log(string action, string entity, string? detail = null)
        {
            try
            {
                _db.ExecuteNonQuery(
                    @"INSERT INTO AuditLog (UserId, Action, Entity, Timestamp, Detail)
                      VALUES (@u, @a, @e, @t, @d);",
                    new Dictionary<string, object?>
                    {
                        ["u"] = Session.CurrentUser?.Id,
                        ["a"] = action,
                        ["e"] = entity,
                        ["t"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ["d"] = detail
                    });
            }
            catch { /* auditing must never break the operation it records */ }
        }

        public DataTable Recent(int limit = 200) => _db.ExecuteQuery(
            $@"SELECT a.Timestamp AS [When],
                      COALESCE(u.FirstName||' '||u.LastName, 'system') AS [User],
                      a.Action, a.Entity, a.Detail
               FROM AuditLog a LEFT JOIN Users u ON u.Id=a.UserId
               ORDER BY a.Id DESC LIMIT {limit};");
    }
}
