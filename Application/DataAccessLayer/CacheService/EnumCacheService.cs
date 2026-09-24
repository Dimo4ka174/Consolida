using Application.DataAccessLayer.Interface.Common;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Application.DataAccessLayer.CacheService
{
    public class EnumCacheService : IEnumCacheService
    {
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

        public EnumCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public List<SelectListItem> GetCachedEnumList<TEnum>() where TEnum : struct, Enum
        {
            string cacheKey = typeof(TEnum).FullName!; // Уникальный ключ для каждого enum

            if (_cache.TryGetValue(cacheKey, out List<SelectListItem>? cachedList))
            {
                return cachedList ?? new List<SelectListItem>();
            }

            var list = Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Select(e => new SelectListItem
                {
                    Text = GetDisplayName(e), // Берём Display.Name или имя enum
                    Value = Convert.ToInt32(e).ToString()
                })
                .ToList();

            _cache.Set(cacheKey, list, CacheDuration);
            return list;
        }

        private static string GetDisplayName<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            var field = value.GetType().GetField(value.ToString());
            var displayAttribute = field?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

            return displayAttribute?.Name ?? value.ToString();
        }
    }
}
