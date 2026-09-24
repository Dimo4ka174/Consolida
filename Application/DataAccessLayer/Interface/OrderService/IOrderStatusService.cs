using Application.ViewModels.OrderModel.Api;

namespace Application.DataAccessLayer.Interface.OrderService
{
    public interface IOrderStatusService
    {
        Task<UpdateOrderStatusResult> UpdateStatusAsync(int orderId, int statusId, string changedBy, CancellationToken ct = default);
        Task<List<StatusHistoryItemDto>> GetHistoryAsync(int orderId, CancellationToken ct = default);
    }
}
