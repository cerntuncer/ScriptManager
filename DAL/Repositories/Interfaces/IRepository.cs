using DAL.Common;
using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<List<T>> GetAllAsync();
        Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> predicate);
        Task<T?> GetByIdAsync(long id);
        Task<T?> GetFirstAsync(Expression<Func<T, bool>> predicate);

        Task AddAsync(T entity);
        Task AddRangeAsync(List<T> entities);

        void Update(T entity);

        Task SoftDeleteAsync(long id);
        Task HardDeleteAsync(long id);//Entity alarak düzenle !!!!!!!!!!!!!!!!!!!!!

        Task<int> SaveAsync();

    }
}