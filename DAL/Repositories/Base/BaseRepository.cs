using DAL.Common;
using DAL.Context;
using DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DAL.Repositories.Base
{
    public class BaseRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly MyContext _context;
        protected readonly DbSet<T> _table;

        public BaseRepository(MyContext context)
        {
            _context = context;
            _table = _context.Set<T>();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _table.ToListAsync();
        }

        public async Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> predicate)
        {
            return await _table.Where(predicate).ToListAsync();
        }

        public async Task<T?> GetByIdAsync(long id)
        {
            return await _table.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<T?> GetFirstAsync(Expression<Func<T, bool>> predicate)
        {
            return await _table.FirstOrDefaultAsync(predicate);
        }

        public async Task AddAsync(T entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsDeleted = false;

            await _table.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.IsDeleted = false;
            }

            await _table.AddRangeAsync(entities);
        }

        public void Update(T entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _table.Update(entity);
        }

        public async Task SoftDeleteAsync(long id)
        {
            var entity = await _table.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            _table.Update(entity);
        }

        public async Task HardDeleteAsync(T entity)
        {
            if (entity == null) return;

           
            var tracked = _context.Entry(entity);
            if (tracked.State == EntityState.Detached)
            {
                var existing = await _table.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Id == entity.Id);
                if (existing == null) return;
                _table.Remove(existing);
                return;
            }

            _table.Remove(entity);
        }

        public async Task HardDeleteByIdAsync(long id)
        {
            var entity = await _table.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) return;

            _table.Remove(entity);
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}