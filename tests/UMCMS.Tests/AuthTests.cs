using System.IO;
using UMCMS.Data;
using UMCMS.Data.Repositories;
using UMCMS.Services;
using Xunit;

namespace UMCMS.Tests
{
    public class AuthTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly DatabaseService _db;
        private readonly UserRepository _users;

        public AuthTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "umcms_auth_" + Guid.NewGuid().ToString("N") + ".db");
            _db = new DatabaseService(_dbPath);
            _db.InitializeDatabase();           // seeds admin / admin123
            _users = new UserRepository(_db);
        }

        private AuthService New() => new(_users);

        [Fact]
        public void Authenticate_Succeeds_WithCorrectCredentials()
        {
            var r = New().Authenticate("admin", "admin123", "Admin");
            Assert.True(r.Success);
            Assert.Equal("admin", r.User!.Username);
        }

        [Fact]
        public void Authenticate_Fails_OnWrongRoleTab()
        {
            var r = New().Authenticate("admin", "admin123", "Doctor");
            Assert.False(r.Success);
        }

        [Fact]
        public void Authenticate_LocksOut_AfterFiveFailures()
        {
            var auth = New();
            for (int i = 0; i < 4; i++)
            {
                var r = auth.Authenticate("admin", "wrong", "Admin");
                Assert.False(r.Success);
                Assert.Contains("remaining", r.Error, StringComparison.OrdinalIgnoreCase);
            }
            // 5th failure trips the lock
            Assert.Contains("locked", auth.Authenticate("admin", "wrong", "Admin").Error, StringComparison.OrdinalIgnoreCase);
            // even the correct password is refused while locked
            var locked = auth.Authenticate("admin", "admin123", "Admin");
            Assert.False(locked.Success);
            Assert.Contains("locked", locked.Error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ChangePassword_Works_AndOldPasswordStopsWorking()
        {
            Assert.Null(New().ChangePassword("admin", "admin123", "newpass1"));
            Assert.True(New().Authenticate("admin", "newpass1", "Admin").Success);
            Assert.False(New().Authenticate("admin", "admin123", "Admin").Success);
        }

        [Fact]
        public void ChangePassword_Rejects_WrongCurrentOrShortNew()
        {
            Assert.NotNull(New().ChangePassword("admin", "WRONG", "newpass1"));
            Assert.NotNull(New().ChangePassword("admin", "admin123", "no"));
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { File.Delete(_dbPath); } catch { }
        }
    }
}
