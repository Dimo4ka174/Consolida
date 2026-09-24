using Application.ViewModels.OrderModel.Api;

namespace Application.DataAccessLayer.Interface.OrderService
{
    public interface IOrderLookupService
    {
        Task<List<CodeLookupDto>> SearchCodesAsync(string query, CancellationToken ct = default);
        Task<MetrologicalLookupDto> SearchMetrologicalInfoAsync(string number, CancellationToken ct = default);
        Task<List<TaxTypeLookupDto>> SearchTaxTypesAsync(string query, CancellationToken ct = default);
        Task<List<CompanyLookupDto>> SearchCompaniesAsync(string query, CancellationToken ct = default);
        Task<List<CustomerLookupDto>> GetCustomersByCompanyAsync(int companyId, CancellationToken ct = default);
    }
}
