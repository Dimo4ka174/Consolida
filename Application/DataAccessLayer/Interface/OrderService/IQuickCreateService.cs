using Application.ViewModels.OrderModel.Api;

namespace Application.DataAccessLayer.Interface.OrderService
{
    public interface IQuickCreateService
    {
        Task<CreatedCompanyDto> CreateCompanyAsync(string name, CancellationToken ct = default);
        Task<CreatedCustomerDto> CreateCustomerAsync(int? companyId, string firstName, string lastName, CancellationToken ct = default);
        Task<CreatedManufacturerDto> CreateManufacturerAsync(string name, CancellationToken ct = default);
    }
}
