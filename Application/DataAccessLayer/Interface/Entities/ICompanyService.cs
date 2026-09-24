using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.CompanyModel;
using DB.Entity;

namespace Application.DataAccessLayer.Interface.Entities
{
    public interface ICompanyService : IGenericService<Company, CompanyDto>
    {
        Task<CompanyDto> GetCreateModelAsync();
        Task<CompanyDto> GetEditModelAsync(int id);
        Task<int> GetRelatedCustomersCountAsync(int companyId);
        Task CreateCompanyAsync(CompanyDto model);
        Task UpdateCompanyAsync(CompanyDto model);
        Task DeleteCompanyAsync(int id);
    }
}
