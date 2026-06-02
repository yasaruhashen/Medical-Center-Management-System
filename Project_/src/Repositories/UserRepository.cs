using Project_.src.Models;

namespace Project_.src.Repositories
{
    public class UserRepository : IUserRepository
    {
        public Task<User?> GetByUsernameAsync(string username)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(User user)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(User user)
        {
            throw new NotImplementedException();
        }
    }
}
