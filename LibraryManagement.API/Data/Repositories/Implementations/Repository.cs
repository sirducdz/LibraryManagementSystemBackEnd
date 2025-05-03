using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LibraryManagement.API.Data.Repositories.Implementations
{
    public class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
         where TEntity : class, IEntity<TKey>
         where TKey : IEquatable<TKey>
    {
        protected readonly LibraryDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public Repository(LibraryDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<TEntity>();
        }

        public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        public virtual IQueryable<TEntity> GetAllQueryable(bool trackEntities = false)
        {
            return trackEntities ? _dbSet : _dbSet.AsNoTracking();
        }
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }
        public virtual IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate, bool trackEntities = false)
        {
            return trackEntities ? _dbSet.Where(predicate) : _dbSet.Where(predicate).AsNoTracking();
        }

        public virtual async Task<int> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            // Gọi SaveChanges ngay lập tức
            return await _context.SaveChangesAsync(cancellationToken);
        }

        // Trả về Task<int> thay vì Task
        public virtual async Task<int> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddRangeAsync(entities, cancellationToken);
            // Gọi SaveChanges ngay lập tức
            return await _context.SaveChangesAsync(cancellationToken);
        }

        // Trả về Task<int> thay vì void (cần làm async)
        public virtual async Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) // Đổi tên thành UpdateAsync
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            return await _context.SaveChangesAsync(cancellationToken);
        }
        public virtual async Task<int> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null || !entities.Any())
            {
                return 0; // Không có gì để cập nhật
            }
            // Lặp qua từng entity trong danh sách
            _context.UpdateRange(entities);
            try
            {
                return await _context.SaveChangesAsync(cancellationToken); // Lưu tất cả thay đổi trong một transaction
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync();
                }
                return 0;
            }
        }

        public virtual async Task<int> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default) // Đổi tên thành RemoveAsync
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
            return await _context.SaveChangesAsync(cancellationToken);
        }

        // Trả về Task<int> thay vì void (cần làm async)
        public virtual async Task<int> RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) // Đổi tên thành RemoveRangeAsync
        {
            _dbSet.RemoveRange(entities);
            // Gọi SaveChanges ngay lập tức
            return await _context.SaveChangesAsync(cancellationToken);
        }
        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(predicate, cancellationToken);
        }

    }
}
