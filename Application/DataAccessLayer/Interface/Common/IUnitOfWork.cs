using DB.Abstract;
using DB.Entity;

namespace Application.DataAccessLayer.Interface.Common
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        IRepository<T> GetRepository<T>() where T : class, IEntity;

        IRepository<City> Cities { get; }
        IRepository<Company> Companies { get; }
        IRepository<Customer> Customers { get; }
        IRepository<Order> Orders { get; }
        IRepository<OrderNotification> OrderNotifications { get; }
        IRepository<ConsolidationPool> ConsolidationPools { get; }
        IRepository<ConsolidationWeightLimit> ConsolidationWeightLimits { get; }
        IRepository<ConsolidationPoolHistory> ConsolidationPoolHistories { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
        bool HasActiveTransaction { get; }
    }
}
