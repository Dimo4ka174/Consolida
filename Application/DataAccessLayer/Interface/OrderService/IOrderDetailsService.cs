using Application.ViewModels.OrderModel;

namespace Application.DataAccessLayer.Interface.OrderService
{
    public interface IOrderDetailsService
    {
        Task<OrderDetailsViewModel?> GetOrderDetailsAsync(int orderId);
    }
}