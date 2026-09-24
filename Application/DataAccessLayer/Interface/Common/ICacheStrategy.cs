namespace Application.DataAccessLayer.Interface.Common
{
    /// <summary>
    /// Стратегия кэширования. Strategy — мы можем подставить любую реализацию (MemoryCache или Redis), и кэш-сервис будет работать одинаково.
    /// </summary>
    public interface ICacheStrategy<T>
    {
        /// <summary>
        /// Получить данные из кэша или загрузить через фабрику
        /// </summary>
        Task<IEnumerable<T>> GetAsync(
            string key,
            Func<Task<IEnumerable<T>>> factory,
            TimeSpan expiration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохранить данные в кэш
        /// </summary>
        Task SetAsync(
            string key,
            IEnumerable<T> value,
            TimeSpan expiration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Удалить данные из кэша
        /// </summary>
        Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default);
    }
}
