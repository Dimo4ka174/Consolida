namespace Application.DataAccessLayer.Interface.Common
{
    public interface ICascadeSoftDeleteService
    {
        Task DeleteCity(int cityId, CancellationToken ct = default);
        Task DeleteCompany(int companyId, CancellationToken ct = default);
        Task DeleteCustomer(int customerId, CancellationToken ct = default);
    }
}
