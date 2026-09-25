using Application.DataAccessLayer.Interface.Entities;
using Application.ViewModels.OrderNotificationModel;
using Application.DataAccessLayer.Interface.Common;
using Microsoft.EntityFrameworkCore;
using DB.Entity;

namespace Application.DataAccessLayer.Service.Entity
{
    public class OrderNotificationService : IOrderNotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderNotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> CheckOrderExistsAsync(int orderId)
        {
            return await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .AnyAsync(o => o.Id == orderId && !o.IsDeleted);
        }

        public async Task<List<OrderNotificationViewModel>> GetForOrderAsync(int orderId)
        {
            var list = await _unitOfWork.GetRepository<OrderNotification>()
                .GetQueryable()
                .Include(n => n.Order)
                .Where(n => n.OrderId == orderId && !n.IsDeleted)
                .OrderBy(n => n.IsCompleted)
                .ThenBy(n => n.DueDate)
                .ToListAsync();

            return list.Select(Map).ToList();
        }

        public async Task<List<OrderNotificationViewModel>> GetUrgentAsync(int daysAhead = 7)
        {
            var threshold = DateTime.Today.AddDays(daysAhead);
            var list = await _unitOfWork.GetRepository<OrderNotification>()
                .GetQueryable()
                .Include(n => n.Order)
                .Where(n => !n.IsDeleted && !n.IsCompleted && n.DueDate.Date <= threshold)
                .OrderBy(n => n.DueDate)
                .ToListAsync();

            return list.Select(Map).ToList();
        }

        public async Task<List<OrderNotificationViewModel>> GetAllAsync(bool includeCompleted = false)
        {
            var query = _unitOfWork.GetRepository<OrderNotification>()
                .GetQueryable()
                .Include(n => n.Order)
                .Where(n => !n.IsDeleted);

            if (!includeCompleted)
                query = query.Where(n => !n.IsCompleted);

            var list = await query
                .OrderBy(n => n.IsCompleted)
                .ThenBy(n => n.DueDate)
                .ToListAsync();

            return list.Select(Map).ToList();
        }

        public async Task<int> CreateAsync(OrderNotificationCreateDto dto, string userName)
        {
            var entity = new OrderNotification
            {
                OrderId = dto.OrderId,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                DueDate = dto.DueDate.Date,
                CreatedAt = DateTime.Now,
                CreatedBy = userName,
                IsCompleted = false,
                IsDeleted = false
            };
            await _unitOfWork.GetRepository<OrderNotification>().Create(entity);
            await _unitOfWork.SaveChangesAsync();
            return entity.Id ?? 0;
        }

        public async Task<bool> CompleteAsync(int id, string userName)
        {
            var entity = await _unitOfWork.GetRepository<OrderNotification>().GetById(id);
            if (entity == null || entity.IsDeleted) return false;

            entity.IsCompleted = true;
            entity.CompletedAt = DateTime.Now;
            entity.CompletedBy = userName;
            _unitOfWork.GetRepository<OrderNotification>().Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _unitOfWork.GetRepository<OrderNotification>().GetById(id);
            if (entity == null) return false;

            entity.IsDeleted = true;
            _unitOfWork.GetRepository<OrderNotification>().Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static OrderNotificationViewModel Map(OrderNotification n) => new()
        {
            Id = n.Id,
            OrderId = n.OrderId ?? 0,
            OrderNumber = n.Order != null ? n.Order.OrderNumber : string.Empty,
            Title = n.Title,
            Description = n.Description,
            DueDate = n.DueDate,
            CreatedAt = n.CreatedAt,
            CreatedBy = n.CreatedBy,
            IsCompleted = n.IsCompleted,
            CompletedAt = n.CompletedAt,
            CompletedBy = n.CompletedBy
        };

        public async Task<List<OrderSearchResultDto>> SearchOrdersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<OrderSearchResultDto>();

            return await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .Where(o => !o.IsDeleted && EF.Functions.ILike(o.OrderNumber, $"%{query}%"))
                .OrderByDescending(o => o.Id)
                .Select(o => new OrderSearchResultDto
                {
                    Id = o.Id ?? 0,
                    OrderNumber = o.OrderNumber
                })
                .Take(10)
                .ToListAsync();
        }

        public async Task<bool> RestoreAsync(int id, string userName)
        {
            var entity = await _unitOfWork.GetRepository<OrderNotification>().GetById(id);
            if (entity == null || entity.IsDeleted) return false;

            entity.IsCompleted = false;
            entity.CompletedAt = null;
            entity.CompletedBy = null;
            _unitOfWork.GetRepository<OrderNotification>().Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
