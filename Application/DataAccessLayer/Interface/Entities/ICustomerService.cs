using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.CustomerModel;
using DB.Entity;

namespace Application.DataAccessLayer.Interface.Entities
{
    public interface ICustomerService : IGenericService<Customer, CustomerDto>
    {
        Task<PagedResult<CustomerDto>> GetFilteredPagedAsync(FilterParams parameters);
        Task<CustomerDto> GetCreateModelAsync();
        Task<CustomerDto> GetEditModelAsync(int id);
    }
}
