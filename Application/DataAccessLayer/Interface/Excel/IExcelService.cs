using Application.ViewModels.OrderModel.Products;
using Microsoft.AspNetCore.Http;

namespace Application.DataAccessLayer.Interface.Excel
{
    public interface IExcelService
    {
        List<ExcelProduct> ParseProductsFromExcel(IFormFile file);
    }
}
