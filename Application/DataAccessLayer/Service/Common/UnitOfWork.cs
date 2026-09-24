using Application.DataAccessLayer.Interface.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using DB.Abstract;
using DB.Entity;
using DB;

namespace Application.DataAccessLayer.Service.Common
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<Type, object> _repositories = new();

        public UnitOfWork(AppDbContext context, IServiceProvider serviceProvider)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        // Реализация универсального доступа к репозиториям
        public IRepository<T> GetRepository<T>() where T : class, IEntity
        {
            return (IRepository<T>)_repositories.GetOrAdd(typeof(T),
                _ => ActivatorUtilities.CreateInstance<GenericRepository<T>>(_serviceProvider, _context));
        }

        // Явные свойства для удобства
        public IRepository<City> Cities => GetRepository<City>();
        public IRepository<Company> Companies => GetRepository<Company>();
        public IRepository<Customer> Customers => GetRepository<Customer>();


        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _context.SaveChangesAsync(ct);
        }
        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                await action();
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                T result = await action();
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public bool HasActiveTransaction => _context.Database.CurrentTransaction != null;

        public void Dispose()
        {
            foreach (var repository in _repositories.Values)
            {
                if (repository is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _repositories.Clear();

            _context.Dispose();
        }
        public async ValueTask DisposeAsync()
        {
            foreach (var repository in _repositories.Values)
            {
                if (repository is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
            }
            _repositories.Clear();

            await _context.DisposeAsync();
        }
    }
}
