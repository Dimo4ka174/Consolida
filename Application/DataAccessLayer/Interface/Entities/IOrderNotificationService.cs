using Application.ViewModels.OrderNotificationModel;

namespace Application.DataAccessLayer.Interface.Entities
{
    public interface IOrderNotificationService
    {
        Task<List<OrderNotificationViewModel>> GetForOrderAsync(int orderId);
        Task<List<OrderNotificationViewModel>> GetUrgentAsync(int daysAhead = 7);
        Task<List<OrderNotificationViewModel>> GetAllAsync(bool includeCompleted = false);
        Task<int> CreateAsync(OrderNotificationCreateDto dto, string userName);
        Task<bool> CompleteAsync(int id, string userName);
        Task<bool> DeleteAsync(int id);
        Task<bool> CheckOrderExistsAsync(int orderId);
        Task<bool> RestoreAsync(int id, string userName);
        Task<List<OrderSearchResultDto>> SearchOrdersAsync(string query);
    }
}
