using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Data.Repositories.Implementations
{
    public class UserRepository : Repository<User, int>, IUserRepository
    {
        public UserRepository(LibraryDbContext context) : base(context) { }

        // Implement các phương thức đặc thù
        public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await GetAllQueryable(true).FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            return await GetAllQueryable(true).FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
        }

        //public async Task<User?> FindByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        //{
        //    return await GetAllQueryable()
        //               .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, cancellationToken);
        //}

        // Override các phương thức generic nếu cần hành vi khác cho User
        // Ví dụ: luôn Include Role khi lấy User
        // public override async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        // {
        //     return await GetQueryable()
        //                .Include(u => u.Role) // Giả sử có navigation property Role
        //                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        // }
    }
}
