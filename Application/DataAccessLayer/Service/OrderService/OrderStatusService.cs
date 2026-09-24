using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.OrderService;
using Application.ViewModels.OrderModel.Api;
using DB.Entity;
using DB.Entity.Enum;
using Microsoft.EntityFrameworkCore;

namespace Application.DataAccessLayer.Service.OrderService
{
    public class OrderStatusService : IOrderStatusService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderStatusService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UpdateOrderStatusResult> UpdateStatusAsync(int orderId, int statusId, string changedBy, CancellationToken ct = default)
        {
            var order = await _unitOfWork.GetRepository<Order>().GetById(orderId);
            if (order == null)
                return new UpdateOrderStatusResult { Success = false, Message = "Заказ не найден" };

            var oldStatus = order.Status;
            var newStatus = (Status)statusId;

            if (oldStatus == newStatus)
                return new UpdateOrderStatusResult { Success = true, Message = "Статус не изменился" };

            order.Status = newStatus;
            order.LastStatusChangeDate = DateTime.Now;
            _unitOfWork.GetRepository<Order>().Update(order);

            var history = new OrderStatusHistory
            {
                OrderId = order.Id,
                ChangeDate = DateTime.Now,
                ChangedBy = changedBy,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                IsDeleted = false
            };

            await _unitOfWork.GetRepository<OrderStatusHistory>().Create(history);
            await _unitOfWork.SaveChangesAsync(ct);

            return new UpdateOrderStatusResult
            {
                Success = true,
                Message = "Статус успешно обновлен",
                OldStatus = oldStatus.ToString(),
                NewStatus = newStatus.ToString(),
                ChangeDate = DateTime.Now,
                ChangedBy = changedBy
            };
        }

        public async Task<List<StatusHistoryItemDto>> GetHistoryAsync(int orderId, CancellationToken ct = default)
        {
            var history = await _unitOfWork.GetRepository<OrderStatusHistory>()
                .GetQueryable()
                .Where(h => h.OrderId == orderId && !h.IsDeleted)
                .OrderByDescending(h => h.ChangeDate)
                .ToListAsync(ct);

            return history.Select(h => new StatusHistoryItemDto
            {
                Id = h.Id,
                OldStatus = h.OldStatus.ToString(),
                OldStatusDisplay = GetDisplayName(h.OldStatus),
                NewStatus = h.NewStatus.ToString(),
                NewStatusDisplay = GetDisplayName(h.NewStatus),
                ChangeDate = h.ChangeDate.ToString("dd/MM/yy HH:mm"),
                ChangedBy = h.ChangedBy
            }).ToList();
        }

        private static string GetDisplayName(Status status)
        {
            var field = status.GetType().GetField(status.ToString());
            var attribute = field?.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                .FirstOrDefault() as System.ComponentModel.DataAnnotations.DisplayAttribute;
            return attribute?.Name ?? status.ToString();
        }
    }
}
