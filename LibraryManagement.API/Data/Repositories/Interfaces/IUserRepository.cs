using LibraryManagement.API.Models.Entities;

namespace LibraryManagement.API.Data.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<User, int>
    {
        Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> FindByUserNameAsync(string email, CancellationToken cancellationToken = default);
        //Task<User?> FindByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    }
}
