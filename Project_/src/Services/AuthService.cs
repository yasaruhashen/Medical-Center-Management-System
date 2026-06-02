using Project_.src.Models;

namespace Project_.src.Services
{
    public class AuthService
    {
        public AuthService()
        {
        }

        public bool AuthenticateUser(string username, string password)
        {
            // Verify username and password hash
            throw new NotImplementedException();
        }

        public string HashPassword(string password)
        {
            // Secure hashing of passwords
            throw new NotImplementedException();
        }
    }
}
