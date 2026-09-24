using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.ManufacturerModel;
using AutoMapper;
using DB.Entity;

namespace Application.DataAccessLayer.Service.Entity
{
    public class ManufacturerService : GenericService<Manufacturer, ManufacturerDto>, IManufacturerService
    {
        public ManufacturerService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFilterService<Manufacturer> filterService,
            ICacheService<Manufacturer> cacheService)
            : base(unitOfWork, mapper, filterService, cacheService)
        {
        }

        public override async Task<PagedResult<ManufacturerDto>> GetPagedAsync(FilterParams parameters, CancellationToken ct = default)
        {
            var cached = await Cache!.GetCachedDataAsync(ct);
            var result = FilterService.ApplyFilters(cached, parameters);
            return new PagedResult<ManufacturerDto>
            {
                Items = Mapper.Map<List<ManufacturerDto>>(result.Data),
                TotalItems = result.TotalItems,
                ErrorMessage = result.ErrorMessage
            };
        }
    }
}
