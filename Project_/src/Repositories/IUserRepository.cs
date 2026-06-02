using Project_.src.Models;

namespace Project_.src.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
    }
}
