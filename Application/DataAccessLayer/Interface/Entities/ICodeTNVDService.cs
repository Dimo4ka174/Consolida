using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.CodeTNVDModel;
using DB.Entity;

namespace Application.DataAccessLayer.Interface.Entities
{
    public interface ICodeTNVDService : IGenericService<CodeTNVD, CodeTNVDdto>
    {
        Task<PagedResult<CodeTNVDdto>> GetPagedAsync(FilterParams parameters, CancellationToken ct = default);
    }
}
