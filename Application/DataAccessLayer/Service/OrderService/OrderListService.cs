using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.DataAccessLayer.CacheService;
using Microsoft.AspNetCore.Mvc.Rendering;
using Application.ViewModels.OrderModel;
using Microsoft.EntityFrameworkCore;
using DB.Entity.Enum;
using AutoMapper;
using Application.DataAccessLayer.Interface.OrderService;

namespace Application.DataAccessLayer.Service.OrderService
{
    public class OrderListService : IOrderListService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IEnumCacheService _enumCacheService;

        public OrderListService(IUnitOfWork unitOfWork, IMapper mapper, IEnumCacheService enumCacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _enumCacheService = enumCacheService;
        }

        public async Task<PagedResult<OrderListDto>> GetOrdersAsync(OrderFilterParams filter)
        {
            var query = _unitOfWork.GetRepository<DB.Entity.Order>()
                .GetQueryableWithIncludes(o => o.Customer, o => o.Customer.Company);

            if (filter.Status.HasValue)
                query = query.Where(o => o.Status == filter.Status.Value);
            else
                query = query.Where(o => o.Status != Status.Archive);

            // Фильтры
            if (!string.IsNullOrEmpty(filter.SearchOrderNumber))
                query = query.Where(o => EF.Functions.ILike(o.OrderNumber, $"%{filter.SearchOrderNumber}%"));

            if (!string.IsNullOrEmpty(filter.SearchCompanyName))
                query = query.Where(o => EF.Functions.ILike(o.Customer.Company.Name, $"%{filter.SearchCompanyName}%"));

            if (filter.Priority.HasValue)
                query = query.Where(o => o.Priority == filter.Priority.Value);

            if (filter.SearchDateFrom.HasValue)
                query = query.Where(o => o.CreationDate >= filter.SearchDateFrom.Value);

            if (filter.SearchDateTo.HasValue)
                query = query.Where(o => o.CreationDate <= filter.SearchDateTo.Value);

            if (filter.MinAmount.HasValue)
                query = query.Where(o => o.TotalCost >= filter.MinAmount.Value);

            if (filter.MaxAmount.HasValue)
                query = query.Where(o => o.TotalCost <= filter.MaxAmount.Value);

            // Сортировка
            query = filter.SortOrder switch
            {
                "Number_desc" => query.OrderByDescending(o => o.OrderNumber),
                "Number" => query.OrderBy(o => o.OrderNumber),
                "Company_desc" => query.OrderByDescending(o => o.Customer.Company.Name),
                "Company" => query.OrderBy(o => o.Customer.Company.Name),
                "Weight_desc" => query.OrderByDescending(o => o.TotalWeight),
                "Weight" => query.OrderBy(o => o.TotalWeight),
                "Cost_desc" => query.OrderByDescending(o => o.TotalCost),
                "Cost" => query.OrderBy(o => o.TotalCost),
                "Priority_desc" => query.OrderByDescending(o => o.Priority),
                "Priority" => query.OrderBy(o => o.Priority),
                "Date" => query.OrderBy(o => o.LastChangeDate),
                _ => query.OrderByDescending(o => o.LastChangeDate)
            };

            var totalItems = await query.CountAsync();
            var orders = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var models = _mapper.Map<List<OrderListDto>>(orders);

            return new PagedResult<OrderListDto>
            {
                Items = models,
                TotalItems = totalItems
            };
        }

        public Task<List<SelectListItem>> GetPriorityListAsync()
        {
            return Task.FromResult(_enumCacheService.GetCachedEnumList<Priority>());
        }

        public Task<List<SelectListItem>> GetStatusListAsync()
        {
            return Task.FromResult(_enumCacheService.GetCachedEnumList<Status>());
        }
    }
}
