using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.ProductModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using DB.Entity;

namespace Application.DataAccessLayer.Interface.Entities
{
    public interface IProductService : IGenericService<Product, ProductDto>
    {
        Task<PagedResult<ProductDto>> GetFilteredPagedAsync(
            int page, int pageSize,
            string? searchString, string? searchManufacturer,
            string? sortOrder);

        Task<ProductDto> GetCreateModelAsync();
        Task<ProductDto> GetEditModelAsync(int id);
        Task<List<SelectListItem>> GetManufacturersSelectListAsync();
    }
}
