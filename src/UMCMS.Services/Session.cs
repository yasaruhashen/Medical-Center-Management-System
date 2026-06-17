using UMCMS.Domain.Entities;

namespace UMCMS.Services
{
    /// <summary>Holds the currently authenticated user for the running app session.</summary>
    public static class Session
    {
        public static User? CurrentUser { get; private set; }
        public static bool IsLoggedIn => CurrentUser is not null;
        public static void SignIn(User user) => CurrentUser = user;
        public static void SignOut() => CurrentUser = null;
    }
}
