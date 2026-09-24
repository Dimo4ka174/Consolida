using Application.ViewModels.OrderModel;

namespace Application.DataAccessLayer.Interface.OrderService
{
    public interface IOrderDuplicateService
    {
        Task<OrderDuplicateResult> DuplicateOrderAsync(DuplicateOrderViewModel model);
    }
}
