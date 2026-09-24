using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.TaxTypeModel;
using DB.Entity;

namespace Application.DataAccessLayer.Interface.Entities
{
    public interface ITaxTypeService : IGenericService<TaxType, TaxTypeDto>
    {
        Task<PagedResult<TaxTypeDto>> GetPagedAsync(FilterParams parameters, CancellationToken ct = default);
        Task<TaxTypeDto> GetCreateModelAsync();
        Task<TaxTypeDto> GetEditModelAsync(int id);
    }
}
