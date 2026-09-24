using Application.ViewModels.OrderModel;
using DB.Entity;

namespace Application.DataAccessLayer.Interface.OrderService
{
    public interface IOrderSaveService
    {
        Task<OrderSaveResult> SaveOrderDataAsync(ExportOrderViewModel model);
        Task<Stream> GenerateTKPAsync(ExportOrderViewModel model, Order order);
    }

    public class OrderSaveResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Order? Order { get; set; }
        public string OrderNumber => Order?.OrderNumber;
    }
}
