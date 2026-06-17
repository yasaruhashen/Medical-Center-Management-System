using UMCMS.Data.Repositories;
using UMCMS.Domain.Entities;

namespace UMCMS.Services
{
    /// <summary>
    /// Authenticates users with BCrypt hashing, applies failed-attempt lockout (FR-AUTH-4),
    /// and supports self-service password change (FR-AUTH-5).
    /// </summary>
    public class AuthService
    {
        public const int MaxAttempts = 5;
        public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);

        private readonly IUserRepository _users;
        private readonly Dictionary<string, (int Count, DateTime? LockedUntil)> _attempts =
            new(StringComparer.OrdinalIgnoreCase);

        public AuthService(IUserRepository users) => _users = users;

        /// <summary>Result of an authentication attempt.</summary>
        public sealed record Result(User? User, string? Error)
        {
            public bool Success => User is not null;
        }

        public Result Authenticate(string username, string password, string expectedRole)
        {
            username = (username ?? string.Empty).Trim();

            // Locked out?
            if (_attempts.TryGetValue(username, out var st) && st.LockedUntil is DateTime until && until > DateTime.Now)
            {
                var mins = Math.Max(1, Math.Ceiling((until - DateTime.Now).TotalMinutes));
                return new Result(null, $"Account locked due to failed attempts. Try again in {mins} minute(s).");
            }

            var user = _users.GetByUsername(username);

            if (user is not null && !user.IsActive)
                return new Result(null, "This account is deactivated. Please contact an administrator.");

            bool ok = user is not null && user.IsActive
                      && string.Equals(user.Role, expectedRole, StringComparison.OrdinalIgnoreCase)
                      && VerifyPassword(password, user.PasswordHash);

            if (ok)
            {
                _attempts.Remove(username);   // reset on success
                return new Result(user, null);
            }

            // Record the failure / escalate to lockout.
            int count = (_attempts.TryGetValue(username, out var s) ? s.Count : 0) + 1;
            if (count >= MaxAttempts)
            {
                _attempts[username] = (count, DateTime.Now.Add(LockDuration));
                return new Result(null, $"Too many failed attempts. Account locked for {LockDuration.TotalMinutes:0} minutes.");
            }

            _attempts[username] = (count, null);
            return new Result(null, $"Invalid credentials for {expectedRole}. {MaxAttempts - count} attempt(s) remaining.");
        }

        /// <summary>Self-service password change. Returns null on success, else an error message.</summary>
        public string? ChangePassword(string username, string currentPassword, string newPassword)
        {
            var user = _users.GetByUsername(username);
            if (user is null) return "User not found.";
            if (!VerifyPassword(currentPassword, user.PasswordHash)) return "Your current password is incorrect.";
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
                return "New password must be at least 4 characters.";

            _users.UpdatePassword(user.Id, HashPassword(newPassword));
            return null;
        }

        public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        public bool VerifyPassword(string password, string hash)
        {
            try { return BCrypt.Net.BCrypt.Verify(password, hash); }
            catch { return false; }
        }
    }
}
