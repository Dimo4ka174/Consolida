using System.Text.Json;
using Application.DataAccessLayer.Interface.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Application.DataAccessLayer.CacheService
{
    /// <summary>
    /// In-memory реализация кэша курса юаня.
    /// Используется, когда Redis не сконфигурирован.
    /// </summary>
    public class MemoryCurrencyCacheService : ICurrencyCacheService
    {
        private const string CacheKey = "CNY_ExchangeRate";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(8);

        private readonly IMemoryCache _memoryCache;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MemoryCurrencyCacheService> _logger;

        public MemoryCurrencyCacheService(
            IMemoryCache memoryCache,
            HttpClient httpClient,
            ILogger<MemoryCurrencyCacheService> logger)
        {
            _memoryCache = memoryCache;
            _httpClient = httpClient;
            _logger = logger;
        }

        public Task<decimal> GetExchangeRateAsync(CancellationToken cancellationToken = default)
        {
            if (_memoryCache.TryGetValue(CacheKey, out decimal cachedRate))
            {
                _logger.LogDebug("Memory cache hit for exchange rate");
                return Task.FromResult(cachedRate);
            }

            return FetchAndCacheAsync(cancellationToken);
        }

        public async Task UpdateCacheAsync(CancellationToken cancellationToken = default)
        {
            var rate = await FetchFromApiAsync(cancellationToken);
            _memoryCache.Set(CacheKey, rate, CacheDuration);
            _logger.LogDebug("Memory cache updated for exchange rate: {Rate}", rate);
        }

        public Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
        {
            _memoryCache.Remove(CacheKey);
            _logger.LogDebug("Memory cache invalidated for exchange rate");
            return Task.CompletedTask;
        }

        private async Task<decimal> FetchAndCacheAsync(CancellationToken cancellationToken)
        {
            var rate = await FetchFromApiAsync(cancellationToken);
            _memoryCache.Set(CacheKey, rate, CacheDuration);
            return rate;
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
