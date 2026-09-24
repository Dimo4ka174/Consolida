using DB.Abstract;

namespace Application.DataAccessLayer.Interface.Common
{
    public interface ICacheService<T> where T : class, IEntity
    {
        /// <summary>
        /// Получить данные из кэша
        /// </summary>
        Task<IEnumerable<T>> GetCachedDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить кэш (принудительно)
        /// </summary>
        Task UpdateCacheAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Обнулить кэш
        /// </summary>
        Task InvalidateCacheAsync(CancellationToken cancellationToken = default);
    }
}
