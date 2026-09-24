using System.Text.Json;
using Application.DataAccessLayer.Interface.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Application.DataAccessLayer.CacheService
{
    /// <summary>
    /// Redis-реализация кэша курса юаня.
    /// Используется, когда задана переменная REDIS_HOST.
    /// </summary>
    public class RedisCurrencyCacheService : ICurrencyCacheService
    {
        private const string CacheKey = "CNY_ExchangeRate";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(8);

        private readonly IDistributedCache _distributedCache;
        private readonly HttpClient _httpClient;
        private readonly ILogger<RedisCurrencyCacheService> _logger;

        public RedisCurrencyCacheService(
            IDistributedCache distributedCache,
            HttpClient httpClient,
            ILogger<RedisCurrencyCacheService> logger)
        {
            _distributedCache = distributedCache;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<decimal> GetExchangeRateAsync(CancellationToken cancellationToken = default)
        {
            var cachedJson = await _distributedCache.GetStringAsync(CacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                _logger.LogDebug("Redis cache hit for exchange rate");
                return JsonSerializer.Deserialize<decimal>(cachedJson);
            }

            return await FetchAndCacheAsync(cancellationToken);
        }

        public async Task UpdateCacheAsync(CancellationToken cancellationToken = default)
        {
            var rate = await FetchFromApiAsync(cancellationToken);
            await SetCacheAsync(rate, cancellationToken);
            _logger.LogDebug("Redis cache updated for exchange rate: {Rate}", rate);
        }

        public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
        {
            await _distributedCache.RemoveAsync(CacheKey, cancellationToken);
            _logger.LogDebug("Redis cache invalidated for exchange rate");
        }

        private async Task<decimal> FetchAndCacheAsync(CancellationToken cancellationToken)
        {
            var rate = await FetchFromApiAsync(cancellationToken);
            await SetCacheAsync(rate, cancellationToken);
            return rate;
        }

        private async Task SetCacheAsync(decimal rate, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(rate);
            await _distributedCache.SetStringAsync(CacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            }, cancellationToken);
        }

        private async Task<decimal> FetchFromApiAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Fetching exchange rate from external API");
                var response = await _httpClient.GetAsync(
                    "https://www.cbr-xml-daily.ru/daily_json.js",
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var json = JsonDocument.Parse(content);
                    var cnyRate = json.RootElement
                        .GetProperty("Valute")
                        .GetProperty("CNY")
                        .GetProperty("Value")
                        .GetDecimal();

                    _logger.LogDebug("Exchange rate fetched: {Rate}", cnyRate);
                    return cnyRate;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching exchange rate from API");
            }

            return 1m;
        }
    }
}
