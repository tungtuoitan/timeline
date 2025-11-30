using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class UserRepository : IUserRepository
    {
        public Task<User?> GetUserByEmailAsync(string email)
        {
            throw new NotImplementedException("UserRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            throw new NotImplementedException("UserRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<User> CreateUserAsync(User user)
        {
            throw new NotImplementedException("UserRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task<User> CreateAsync(User user)
        {
            throw new NotImplementedException("UserRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task UpdateLastLoginAsync(int userId)
        {
            throw new NotImplementedException("UserRepository chưa được implement - cần migrate từ code cũ");
        }

        public Task UpdateAsync(User user)
        {
            throw new NotImplementedException("UserRepository chưa được implement - cần migrate từ code cũ");
        }
    }
}
