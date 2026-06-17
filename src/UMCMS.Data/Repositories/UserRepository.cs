using System.Data;
using UMCMS.Domain.Entities;

namespace UMCMS.Data.Repositories
{
    public interface IUserRepository
    {
        User? GetByUsername(string username);
        User? GetById(int id);
        List<User> GetAll();
        List<User> GetDoctors();
        int Add(User user);
        void Update(User user);
        void UpdatePassword(int id, string passwordHash);
    }

    public class UserRepository : IUserRepository
    {
        private readonly DatabaseService _db;
        public UserRepository(DatabaseService db) => _db = db;

        internal static User Map(IDataReader r) => new()
        {
            Id = Convert.ToInt32(r["Id"]),
            FirstName = r["FirstName"] as string ?? "",
            LastName = r["LastName"] as string ?? "",
            Username = r["Username"] as string ?? "",
            PasswordHash = r["PasswordHash"] as string ?? "",
            Role = r["Role"] as string ?? "",
            Specialization = r["Specialization"] as string,
            PhoneNumber = r["PhoneNumber"] as string ?? "",
            Email = r["Email"] as string ?? "",
            IsActive = Convert.ToInt64(r["IsActive"]) == 1
        };

        public User? GetByUsername(string username) =>
            _db.Query("SELECT * FROM Users WHERE Username=@u LIMIT 1;", Map,
                new Dictionary<string, object?> { ["u"] = username }).FirstOrDefault();

        public User? GetById(int id) =>
            _db.Query("SELECT * FROM Users WHERE Id=@id LIMIT 1;", Map,
                new Dictionary<string, object?> { ["id"] = id }).FirstOrDefault();

        public List<User> GetAll() =>
            _db.Query("SELECT * FROM Users ORDER BY Role, LastName;", Map);

        public List<User> GetDoctors() =>
            _db.Query("SELECT * FROM Users WHERE Role='Doctor' AND IsActive=1 ORDER BY LastName;", Map);

        public int Add(User u) => (int)_db.ExecuteInsertReturnId(
            @"INSERT INTO Users (FirstName,LastName,Username,PasswordHash,Role,Specialization,PhoneNumber,Email,IsActive,CreatedAt)
              VALUES (@f,@l,@u,@p,@r,@s,@ph,@e,@act,@c);",
            new Dictionary<string, object?>
            {
                ["f"] = u.FirstName, ["l"] = u.LastName, ["u"] = u.Username, ["p"] = u.PasswordHash,
                ["r"] = u.Role, ["s"] = u.Specialization, ["ph"] = u.PhoneNumber, ["e"] = u.Email,
                ["act"] = u.IsActive ? 1 : 0, ["c"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

        public void Update(User u) => _db.ExecuteNonQuery(
            @"UPDATE Users SET FirstName=@f,LastName=@l,Role=@r,Specialization=@s,PhoneNumber=@ph,Email=@e,IsActive=@act
              WHERE Id=@id;",
            new Dictionary<string, object?>
            {
                ["f"] = u.FirstName, ["l"] = u.LastName, ["r"] = u.Role, ["s"] = u.Specialization,
                ["ph"] = u.PhoneNumber, ["e"] = u.Email, ["act"] = u.IsActive ? 1 : 0, ["id"] = u.Id
            });

        public void UpdatePassword(int id, string passwordHash) => _db.ExecuteNonQuery(
            "UPDATE Users SET PasswordHash=@p WHERE Id=@id;",
            new Dictionary<string, object?> { ["p"] = passwordHash, ["id"] = id });
    }
}
