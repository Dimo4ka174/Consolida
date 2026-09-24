using Application.ViewModels.ProductModel;

namespace Application.DataAccessLayer.Interface.Entities
{
    public interface IProductApiService
    {
        Task<List<ProductSearchResultDto>> SearchAsync(string query, int limit, CancellationToken ct = default);
        Task<ProductSearchResultDto> CreateProductAsync(CreateProductApiRequest request, CancellationToken ct = default);
        Task<List<ManufacturerLookupDto>> GetManufacturersLookupAsync(CancellationToken ct = default);
        Task<ManufacturerLookupDto> CreateManufacturerIfNotExistsAsync(string name, CancellationToken ct = default);
    }
}
