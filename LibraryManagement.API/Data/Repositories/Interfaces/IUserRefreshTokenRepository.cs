using LibraryManagement.API.Models.Entities;

namespace LibraryManagement.API.Data.Repositories.Interfaces
{
    public interface IUserRefreshTokenRepository : IRepository<UserRefreshToken, Guid>
    {
        Task<UserRefreshToken?> FindByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<List<UserRefreshToken>> FindByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<int> RevokeTokensByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    }
}
