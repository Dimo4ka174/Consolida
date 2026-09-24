using System.Text.Json;
using Application.DataAccessLayer.Interface.Common;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.DataAccessLayer.Service.Common
{
    public class RedisCacheStrategy<T> : ICacheStrategy<T> where T : class
    {
        private readonly IDistributedCache _cache;

        public RedisCacheStrategy(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<IEnumerable<T>> GetAsync(
            string key,
            Func<Task<IEnumerable<T>>> factory,
            TimeSpan expiration,
            CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<List<T>>(cached) ?? new List<T>();
            }

            var data = (await factory()).ToList();
            await SetAsync(key, data, expiration, cancellationToken);
            return data;
        }

        public async Task SetAsync(
            string key,
            IEnumerable<T> value,
            TimeSpan expiration,
            CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            }, cancellationToken);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
    }
}
