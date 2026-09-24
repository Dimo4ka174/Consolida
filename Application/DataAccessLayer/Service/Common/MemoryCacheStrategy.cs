using Application.DataAccessLayer.Interface.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Application.DataAccessLayer.Service.Common
{
    /// <summary>
    /// Реализация стратегии кэширования на базе IMemoryCache.
    /// </summary>
    public class MemoryCacheStrategy<T> : ICacheStrategy<T>
    {
        private readonly IMemoryCache _cache;

        public MemoryCacheStrategy(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<IEnumerable<T>> GetAsync(
            string key,
            Func<Task<IEnumerable<T>>> factory,
            TimeSpan expiration,
            CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out IEnumerable<T>? cached) && cached != null)
                return cached;

            var data = await factory();
            _cache.Set(key, data, expiration);
            return data;
        }

        public Task SetAsync(
            string key,
            IEnumerable<T> value,
            TimeSpan expiration,
            CancellationToken cancellationToken = default)
        {
            _cache.Set(key, value, expiration);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }
    }
}
