using Application.DataAccessLayer.Service.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using Application.ViewModels.OrderModel;
using DB.Entity.Enum;

namespace Application.DataAccessLayer.Interface.OrderService
{
    public interface IOrderListService
    {
        Task<PagedResult<OrderListDto>> GetOrdersAsync(OrderFilterParams filter);
        Task<List<SelectListItem>> GetPriorityListAsync();
        Task<List<SelectListItem>> GetStatusListAsync();
    }

    public class OrderFilterParams
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public string? SearchCompanyName { get; set; }
        public string? SearchOrderNumber { get; set; }
        public Priority? Priority { get; set; }
        public Status? Status { get; set; }
        public DateTime? SearchDateFrom { get; set; }
        public DateTime? SearchDateTo { get; set; }
        public string? SortOrder { get; set; } = "Date_desc";
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
    }
}
