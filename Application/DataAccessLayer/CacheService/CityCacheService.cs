using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using DB.Entity;

namespace Application.DataAccessLayer.CacheService
{
    public class CityCacheService : ICacheService<City>
    {
        private readonly CacheService<City> _cacheService;
        private const string CitiesCacheKey = "Cities";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        public CityCacheService(IUnitOfWork unitOfWork, ICacheStrategy<City> cacheStrategy)
        {
            _cacheService = new CacheService<City>(unitOfWork, cacheStrategy, CitiesCacheKey, CacheDuration);
        }

        public async Task<IEnumerable<City>> GetCachedDataAsync(CancellationToken cancellationToken = default)
            => await _cacheService.GetCachedDataAsync(cancellationToken);

        public async Task UpdateCacheAsync(CancellationToken cancellationToken = default)
            => await _cacheService.UpdateCacheAsync(cancellationToken);

        public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
            => await _cacheService.InvalidateCacheAsync(cancellationToken);
    }
}
