using Application.ViewModels.OrderModel;
using Microsoft.AspNetCore.Http;

namespace Application.DataAccessLayer.Interface.Excel
{
    public interface IExcelOrderProcessingService
    {
        Task<OrderFormDto> ProcessExcelOrderAsync(IFormFile file);
    }
}
