using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Data.Repositories.Implementations
{
    public class UserRefreshTokenRepository : Repository<UserRefreshToken, Guid>, IUserRefreshTokenRepository
    {
        public UserRefreshTokenRepository(LibraryDbContext context) : base(context) { }

        public async Task<UserRefreshToken?> FindByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            // GetQueryable() trả về DbSet chưa tracking
            return await GetAllQueryable(true)
                        .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
        }

        public async Task<List<UserRefreshToken>> FindByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await GetAllQueryable(true)
                       .Where(rt => rt.UserId == userId)
                       .ToListAsync(cancellationToken);
        }

        public async Task<int> RevokeTokensByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            // Tìm tất cả token chưa bị revoke của user
            var tokensToRevoke = (await GetAllAsync(cancellationToken))
                .Where(rt => rt.UserId == userId && !rt.IsRevoked);

            if (!tokensToRevoke.Any())
            {
                return 0; // Không có token nào để thu hồi
            }

            foreach (var token in tokensToRevoke)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }
            await UpdateRangeAsync(tokensToRevoke, cancellationToken);

            return tokensToRevoke.Count();

        }
    }
}
