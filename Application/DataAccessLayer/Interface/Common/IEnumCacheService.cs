using Microsoft.AspNetCore.Mvc.Rendering;

namespace Application.DataAccessLayer.Interface.Common
{
    public interface IEnumCacheService
    {
        List<SelectListItem> GetCachedEnumList<TEnum>() where TEnum : struct, Enum;
    }
}
