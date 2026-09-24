using Application.ViewModels.OrderModel;

namespace Application.DataAccessLayer.Interface.OrderService
{
    public interface IOrderDeleteService
    {
        Task<DeleteOrderViewModel> GetDeleteModelAsync(int id);
        Task<int> GetOrderTaxesCountAsync(int orderId);
    }
}
