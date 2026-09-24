using System.Linq.Expressions;
using Application.DataAccessLayer.Interface.Common;
using DB;
using DB.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Application.DataAccessLayer.Service.Common
{
    public class GenericRepository<T> : IRepository<T> where T : class, IEntity
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;
        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task Create(T entity) => await _dbSet.AddAsync(entity);

        public virtual async Task<T?> GetById(int? Id) => await _dbSet.FirstOrDefaultAsync(x => x.Id == Id && !x.IsDeleted);
        public virtual void Update(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }
        public virtual async Task Delete(int? Id)
        {
            var entity = await GetById(Id);
            if (entity != null)
            {
                entity.IsDeleted = true;
                Update(entity);
            }
        }

        public virtual async Task CreateRange(IEnumerable<T> entities) => await _dbSet.AddRangeAsync(entities);
        public virtual void UpdateRange(IEnumerable<T> entities) => _dbSet.UpdateRange(entities);
        public virtual async Task DeleteRange(IEnumerable<int?> ids)
        {
            await _dbSet
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.IsDeleted, true));
        }

        public virtual async Task<IEnumerable<T>> GetList() => await _dbSet.Where(x => !x.IsDeleted).ToListAsync();
        public virtual async Task<IEnumerable<T>> Find(Expression<Func<T, bool>> predicate) => await _dbSet.Where(predicate).Where(x => !x.IsDeleted).ToListAsync();
        public virtual async Task<T?> FindFirstOrDefault(Expression<Func<T, bool>> predicate) => await _dbSet.Where(predicate).FirstOrDefaultAsync();

        public IQueryable<T> GetQueryable(bool includeDeleted = false)
        {
            return includeDeleted
                ? _dbSet.AsQueryable()
                : _dbSet.Where(x => !x.IsDeleted).AsQueryable();
        }
        public IQueryable<T> GetQueryableWithIncludes(params Expression<Func<T, object>>[] includes)
        {
            var query = GetQueryable();
            return includes.Aggregate(query, (current, include) => current.Include(include));
        }
    }
}
