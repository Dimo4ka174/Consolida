using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using DB.Entity;

namespace Application.DataAccessLayer.CacheService
{
    public class CompanyCacheService : ICacheService<Company>
    {
        private readonly CacheService<Company> _cacheService;
        private const string CompaniesCacheKey = "Companies";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public CompanyCacheService(IUnitOfWork unitOfWork, ICacheStrategy<Company> cacheStrategy)
        {
            _cacheService = new CacheService<Company>(unitOfWork, cacheStrategy, CompaniesCacheKey, CacheDuration);
        }

        public async Task<IEnumerable<Company>> GetCachedDataAsync(CancellationToken cancellationToken = default)
            => await _cacheService.GetCachedDataAsync(cancellationToken);

        public async Task UpdateCacheAsync(CancellationToken cancellationToken = default)
            => await _cacheService.UpdateCacheAsync(cancellationToken);

        public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
            => await _cacheService.InvalidateCacheAsync(cancellationToken);
    }
}
