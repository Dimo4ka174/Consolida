using Microsoft.Extensions.Caching.Memory;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using DB.Entity;

namespace Application.DataAccessLayer.CacheService
{
    public class MeasureUnitCacheService : ICacheService<MeasureUnit>
    {
        private readonly CacheService<MeasureUnit> _cacheService;
        private const string MeasureUnitsCacheKey = "MeasureUnits";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public MeasureUnitCacheService(IUnitOfWork unitOfWork, ICacheStrategy<MeasureUnit> cacheStrategy)
        {
            _cacheService = new CacheService<MeasureUnit>(unitOfWork, cacheStrategy, MeasureUnitsCacheKey, CacheDuration);
        }

        public async Task<IEnumerable<MeasureUnit>> GetCachedDataAsync(CancellationToken cancellationToken = default)
            => await _cacheService.GetCachedDataAsync(cancellationToken);

        public async Task UpdateCacheAsync(CancellationToken cancellationToken = default)
            => await _cacheService.UpdateCacheAsync(cancellationToken);

        public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
            => await _cacheService.InvalidateCacheAsync(cancellationToken);
    }
}
