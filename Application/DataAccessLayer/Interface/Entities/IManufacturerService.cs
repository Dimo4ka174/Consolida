using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.ManufacturerModel;
using DB.Entity;

namespace Application.DataAccessLayer.Interface.Entities
{
    public interface IManufacturerService : IGenericService<Manufacturer, ManufacturerDto>
    {
        Task<PagedResult<ManufacturerDto>> GetPagedAsync(FilterParams parameters, CancellationToken ct = default);
    }
}
