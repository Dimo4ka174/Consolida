using Application.DataAccessLayer.Interface.Common;
using Application.ViewModels.OrderModel;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using DB.Entity;
using Application.DataAccessLayer.Interface.OrderService;

namespace Application.DataAccessLayer.Service.OrderService
{
    public class OrderDeleteService : IOrderDeleteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrderDeleteService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<DeleteOrderViewModel> GetDeleteModelAsync(int id)
        {
            var order = await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .Include(o => o.Customer)
                .Include(o => o.OrdersProducts).ThenInclude(op => op.Product)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (order == null) return null;

            var model = _mapper.Map<DeleteOrderViewModel>(order);
            model.Products = order.OrdersProducts?
                .Where(op => !op.IsDeleted)
                .Select(op => new ProductSummary
                {
                    Name = op.Product?.Name ?? "—",
                    Model = op.Product?.Model ?? "—",
                    Quantity = op.Quantity,
                    Price = op.Price,
                    TotalPrice = op.TotalPrice
                }).ToList() ?? new();

            model.StatusHistory = order.StatusHistory?
                .Where(h => !h.IsDeleted)
                .OrderByDescending(h => h.ChangeDate)
                .Select(h => new StatusHistoryItem
                {
                    ChangeDate = h.ChangeDate,
                    ChangedBy = h.ChangedBy,
                    OldStatus = h.OldStatus.ToString(),
                    NewStatus = h.NewStatus.ToString()
                }).ToList() ?? new();

            return model;
        }

        public async Task<int> GetOrderTaxesCountAsync(int orderId)
        {
            return await _unitOfWork.GetRepository<OrderTax>()
                .GetQueryable()
                .CountAsync(ot => ot.OrderId == orderId && !ot.IsDeleted);
        }
    }
}
