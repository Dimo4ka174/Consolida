using Application.DataAccessLayer.Service.Common;

namespace Application.DataAccessLayer.Interface.Common
{
    public interface IFilterService<T>
    {
        FilterResult<T> ApplyFilters(IQueryable<T> query, FilterParams parameters);
        FilterResult<T> ApplyFilters(IEnumerable<T> collection, FilterParams parameters);
    }
}
