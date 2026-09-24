using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.ProductModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using DB.Entity;

namespace Application.DataAccessLayer.Service.Entity
{
    public class ProductService : GenericService<Product, ProductDto>, IProductService
    {
        private readonly ICacheService<Manufacturer> _manufacturerCacheService;
        private readonly ICascadeSoftDeleteService _cascadeDeleteService;

        public ProductService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFilterService<Product> filterService,
            ICacheService<Manufacturer> manufacturerCacheService,
            ICascadeSoftDeleteService cascadeDeleteService)
            : base(unitOfWork, mapper, filterService, null)
        {
            _manufacturerCacheService = manufacturerCacheService;
            _cascadeDeleteService = cascadeDeleteService;
        }

        public async Task<PagedResult<ProductDto>> GetFilteredPagedAsync(
            int page, int pageSize,
            string? searchString, string? searchManufacturer,
            string? sortOrder)
        {
            var query = UnitOfWork.GetRepository<Product>()
                .GetQueryableWithIncludes(p => p.Manufacturer);

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p =>
                    EF.Functions.ILike(p.Name, $"%{searchString}%") ||
                    EF.Functions.ILike(p.Model, $"%{searchString}%"));
            }
            if (!string.IsNullOrEmpty(searchManufacturer))
            {
                query = query.Where(p => p.Manufacturer.Name == searchManufacturer);
            }

            query = sortOrder switch
            {
                "Name_desc" => query.OrderByDescending(p => p.Name),
                "Model" => query.OrderBy(p => p.Model),
                "Model_desc" => query.OrderByDescending(p => p.Model),
                "Manufacturer" => query.OrderBy(p => p.Manufacturer.Name),
                "Manufacturer_desc" => query.OrderByDescending(p => p.Manufacturer.Name),
                "Cost" => query.OrderBy(p => p.Price),
                "Cost_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderBy(p => p.Name),
            };

            var totalItems = await query.CountAsync();
            var safePage = Math.Max(1, page);
            var items = await query
                .Skip((safePage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var models = Mapper.Map<List<ProductDto>>(items);
            var manufacturers = await _manufacturerCacheService.GetCachedDataAsync();
            foreach (var model in models)
            {
                model.ManufacturersList = manufacturers.Select(m => new SelectListItem
                {
                    Text = m.Name,
                    Value = m.Id.ToString(),
                    Selected = m.Id == model.ManufacturerId
                }).ToList();
            }

            return new PagedResult<ProductDto>
            {
                Items = models,
                TotalItems = totalItems
            };
        }

        public async Task<ProductDto> GetCreateModelAsync()
        {
            var manufacturers = await _manufacturerCacheService.GetCachedDataAsync();
            return new ProductDto
            {
                ManufacturersList = manufacturers.Select(m => new SelectListItem
                {
                    Text = m.Name,
                    Value = m.Id.ToString()
                }).ToList()
            };
        }

        public async Task<ProductDto> GetEditModelAsync(int id)
        {
            var product = await Repository.GetById(id);
            if (product == null) return null;

            var model = Mapper.Map<ProductDto>(product);
            var manufacturers = await _manufacturerCacheService.GetCachedDataAsync();
            model.ManufacturersList = manufacturers.Select(m => new SelectListItem
            {
                Text = m.Name,
                Value = m.Id.ToString(),
                Selected = m.Id == model.ManufacturerId
            }).ToList();
            return model;
        }

        public async Task<List<SelectListItem>> GetManufacturersSelectListAsync()
        {
            var manufacturers = await _manufacturerCacheService.GetCachedDataAsync();
            return manufacturers.Select(m => new SelectListItem
            {
                Text = m.Name,
                Value = m.Id.ToString()
            }).ToList();
        }

        public override async Task DeleteAsync(int? id, CancellationToken ct = default)
        {
            if (id.HasValue)
                await _cascadeDeleteService.DeleteProduct(id.Value);
            else
                await base.DeleteAsync(id, ct);
        }
    }
}
