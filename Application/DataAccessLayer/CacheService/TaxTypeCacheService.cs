using Microsoft.Extensions.Caching.Memory;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using DB.Entity;

namespace Application.DataAccessLayer.CacheService
{
    public class TaxTypeCacheService : ICacheService<TaxType>
    {
        private readonly CacheService<TaxType> _cacheService;
        private const string TaxTypesCacheKey = "TaxTypes";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public TaxTypeCacheService(IUnitOfWork unitOfWork, ICacheStrategy<TaxType> cacheStrategy)
        {
            _cacheService = new CacheService<TaxType>(unitOfWork, cacheStrategy, TaxTypesCacheKey, CacheDuration);
        }

        public async Task<IEnumerable<TaxType>> GetCachedDataAsync(CancellationToken cancellationToken = default)
            => await _cacheService.GetCachedDataAsync(cancellationToken);

        public async Task UpdateCacheAsync(CancellationToken cancellationToken = default)
            => await _cacheService.UpdateCacheAsync(cancellationToken);

        public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
            => await _cacheService.InvalidateCacheAsync(cancellationToken);
    }
}
