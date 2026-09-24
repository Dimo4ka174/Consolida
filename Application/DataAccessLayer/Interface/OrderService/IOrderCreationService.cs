using Application.ViewModels.OrderModel;

namespace Application.DataAccessLayer.Interface.OrderService
{
    public interface IOrderCreationService
    {
        Task<OrderCreationResult> CreateOrderAsync(OrderFormDto model, string userName);
    }

    public class OrderCreationResult
    {
        public bool Success { get; set; }
        public int? OrderId { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}