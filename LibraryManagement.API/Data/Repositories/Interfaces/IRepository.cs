using LibraryManagement.API.Models.Entities;
using System.Linq.Expressions;

namespace LibraryManagement.API.Data.Repositories.Interfaces
{
    public interface IRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
                                                where TKey : IEquatable<TKey>
    {
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
        IQueryable<TEntity> GetAllQueryable(bool trackEntities = false); // Mặc định không track
        Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);
        Task<int> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<int> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default); // Đổi tên + Thêm CancellationToken
        Task<int> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task<int> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default); // Đổi tên + Thêm CancellationToken
        Task<int> RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    }
}
