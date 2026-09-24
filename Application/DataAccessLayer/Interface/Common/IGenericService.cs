using Application.DataAccessLayer.Service.Common;
using DB.Abstract;

namespace Application.DataAccessLayer.Interface.Common
{
    public interface IGenericService<TEntity, TDto>
        where TEntity : class, IEntity
        where TDto : class
    {
        Task<List<TDto>> GetAllAsync(CancellationToken ct = default);
        Task<TDto?> GetByIdAsync(int? id, CancellationToken ct = default);
        Task<PagedResult<TDto>> GetPagedAsync(FilterParams parameters, CancellationToken ct = default);
        Task<TDto> CreateAsync(TDto dto, CancellationToken ct = default);
        Task UpdateAsync(int id, TDto dto, CancellationToken ct = default);
        Task DeleteAsync(int? id, CancellationToken ct = default);
        Task<bool> ExistsAsync(int? id, CancellationToken ct = default);
    }
}
