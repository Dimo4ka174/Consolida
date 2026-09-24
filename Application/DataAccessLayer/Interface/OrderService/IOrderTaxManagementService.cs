using Application.ViewModels.OrderModel.Api;

namespace Application.DataAccessLayer.Interface.OrderService
{
    public interface IOrderTaxManagementService
    {
        Task<UpdateCodeRateResult> UpdateCodeRateAsync(UpdateCodeRateRequest request, CancellationToken ct = default);
        Task<AddTaxToOrderResult> AddTaxToOrderAsync(int orderId, int taxTypeId, CancellationToken ct = default);
        Task<RemoveTaxFromOrderResult> RemoveTaxFromOrderAsync(int orderId, int taxTypeId, CancellationToken ct = default);
    }
}
