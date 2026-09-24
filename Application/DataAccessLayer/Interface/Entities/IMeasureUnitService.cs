using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.MeasureUnitModel;
using DB.Entity;

namespace Application.DataAccessLayer.Interface.Entities
{
    public interface IMeasureUnitService : IGenericService<MeasureUnit, MeasureUnitDto>
    {
        Task<PagedResult<MeasureUnitDto>> GetPagedAsync(FilterParams parameters, CancellationToken ct = default);
    }
}
