using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.Common;
using Application.ViewModels.ProductModel;
using Microsoft.EntityFrameworkCore;
using DB.Entity;

namespace Application.DataAccessLayer.Service.Entity
{
    public class ProductApiService : IProductApiService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductApiService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ProductSearchResultDto>> SearchAsync(string query, int limit, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<ProductSearchResultDto>();

            var lowerQuery = query.ToLowerInvariant();

            return await _unitOfWork.GetRepository<Product>()
                .GetQueryable()
                .Where(p => p.Name.ToLower().Contains(lowerQuery)
                            || p.Model.ToLower().Contains(lowerQuery))
                .OrderBy(p => p.Id)
                .Take(limit)
                .Select(p => new ProductSearchResultDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Model = p.Model,
                    Price = p.Price,
                    ManufacturerId = p.ManufacturerId,
                    ManufacturerName = p.Manufacturer != null ? p.Manufacturer.Name : null
                })
                .ToListAsync(ct);
        }

        public async Task<ProductSearchResultDto> CreateProductAsync(CreateProductApiRequest request, CancellationToken ct = default)
        {
            var product = new Product
            {
                Name = request.Name.Trim(),
                Model = (request.Model ?? string.Empty).Trim(),
                Price = request.Price,
                ManufacturerId = request.ManufacturerId,
                IsDeleted = false
            };

            await _unitOfWork.GetRepository<Product>().Create(product);
            await _unitOfWork.SaveChangesAsync(ct);

            string? manufacturerName = null;
            if (product.ManufacturerId.HasValue)
            {
                var manufacturer = await _unitOfWork.GetRepository<Manufacturer>()
                    .GetById(product.ManufacturerId.Value);
                manufacturerName = manufacturer?.Name;
            }

            return new ProductSearchResultDto
            {
                Id = product.Id,
                Name = product.Name,
                Model = product.Model,
                Price = product.Price,
                ManufacturerId = product.ManufacturerId,
                ManufacturerName = manufacturerName
            };
        }

        public async Task<List<ManufacturerLookupDto>> GetManufacturersLookupAsync(CancellationToken ct = default)
        {
            return await _unitOfWork.GetRepository<Manufacturer>()
                .GetQueryable()
                .OrderBy(m => m.Name)
                .Select(m => new ManufacturerLookupDto { Id = m.Id, Name = m.Name })
                .ToListAsync(ct);
        }

        public async Task<ManufacturerLookupDto> CreateManufacturerIfNotExistsAsync(string name, CancellationToken ct = default)
        {
            var trimmed = name.Trim();

            var existing = await _unitOfWork.GetRepository<Manufacturer>()
                .FindFirstOrDefault(m => m.Name == trimmed);

            if (existing != null)
                return new ManufacturerLookupDto { Id = existing.Id, Name = existing.Name };

            var manufacturer = new Manufacturer { Name = trimmed, IsDeleted = false };
            await _unitOfWork.GetRepository<Manufacturer>().Create(manufacturer);
            await _unitOfWork.SaveChangesAsync(ct);

            return new ManufacturerLookupDto { Id = manufacturer.Id, Name = manufacturer.Name };
        }
    }
}
