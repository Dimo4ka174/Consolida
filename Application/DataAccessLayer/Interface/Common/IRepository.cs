using System.Linq.Expressions;
using DB.Abstract;

namespace Application.DataAccessLayer.Interface.Common
{
    public interface IRepository<T>
        where T : IEntity
    {
        Task Create(T entity);
        Task<T?> GetById(int? Id);
        void Update(T entity);
        Task Delete(int? Id);

        Task CreateRange(IEnumerable<T> entities);
        void UpdateRange(IEnumerable<T> entities);
        Task DeleteRange(IEnumerable<int?> ids);

        Task<IEnumerable<T>> GetList();
        Task<IEnumerable<T>> Find(Expression<Func<T, bool>> expression);
        Task<T?> FindFirstOrDefault(Expression<Func<T, bool>> expression);

        IQueryable<T> GetQueryable(bool includeDeleted = false);
        IQueryable<T> GetQueryableWithIncludes(params Expression<Func<T, object>>[] includes);
    }
}
