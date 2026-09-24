using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using DB.Entity;

namespace Application.DataAccessLayer.CacheService
{
    public class ManufacturerCacheService : ICacheService<Manufacturer>
    {
        private readonly CacheService<Manufacturer> _cacheService;
        private const string ManufacturersCacheKey = "Manufacturers";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

        public ManufacturerCacheService(IUnitOfWork unitOfWork, ICacheStrategy<Manufacturer> cacheStrategy)
        {
            _cacheService = new CacheService<Manufacturer>(unitOfWork, cacheStrategy, ManufacturersCacheKey, CacheDuration);
        }

        public async Task<IEnumerable<Manufacturer>> GetCachedDataAsync(CancellationToken cancellationToken = default)
            => await _cacheService.GetCachedDataAsync(cancellationToken);

        public async Task UpdateCacheAsync(CancellationToken cancellationToken = default)
            => await _cacheService.UpdateCacheAsync(cancellationToken);

        public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
            => await _cacheService.InvalidateCacheAsync(cancellationToken);
    }
}
