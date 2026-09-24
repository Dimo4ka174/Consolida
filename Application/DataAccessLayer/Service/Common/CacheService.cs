using Application.DataAccessLayer.Interface.Common;
using DB.Abstract;

namespace Application.DataAccessLayer.Service.Common
{
    public class CacheService<T> : ICacheService<T> where T : class, IEntity
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheStrategy<T> _cacheStrategy;
        private readonly string _cacheKey;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public CacheService(IUnitOfWork unitOfWork, ICacheStrategy<T> cacheStrategy)
        {
            _unitOfWork = unitOfWork;
            _cacheStrategy = cacheStrategy;
            _cacheKey = $"Cache_{typeof(T).Name}";
        }

        public async Task<IEnumerable<T>> GetCachedDataAsync(CancellationToken cancellationToken = default)
        {
            return await _cacheStrategy.GetAsync(
                _cacheKey,
                () => _unitOfWork.GetRepository<T>().GetList(),
                CacheDuration,
                cancellationToken);
        }

        public async Task UpdateCacheAsync(CancellationToken cancellationToken = default)
        {
            var data = await _unitOfWork.GetRepository<T>().GetList();
            await _cacheStrategy.SetAsync(_cacheKey, data, CacheDuration, cancellationToken);
        }

        public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
        {
            await _cacheStrategy.RemoveAsync(_cacheKey, cancellationToken);
        }
    }
}
