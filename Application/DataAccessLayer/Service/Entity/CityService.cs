using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.CityModel;
using AutoMapper;
using DB.Entity;
using Microsoft.EntityFrameworkCore;

namespace Application.DataAccessLayer.Service.Entity
{
    public class CityService : GenericService<City, CityDto>, ICityService
    {
        private readonly ICacheService<City> _cacheService;
        private readonly IFilterService<City> _filterService;
        private readonly ICascadeSoftDeleteService _cascadeDeleteService;

        public CityService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService<City> cacheService,
            IFilterService<City> filterService,
            ICascadeSoftDeleteService cascadeDeleteService)
            : base(unitOfWork, mapper, filterService, cacheService)
        {
            _cacheService = cacheService;
            _filterService = filterService;
            _cascadeDeleteService = cascadeDeleteService;
        }

        public override async Task<PagedResult<CityDto>> GetPagedAsync(FilterParams parameters, CancellationToken ct = default)
        {
            // Используем кэш и фильтрацию
            var cachedCities = await _cacheService.GetCachedDataAsync(ct);
            var result = _filterService.ApplyFilters(cachedCities, parameters);
            return new PagedResult<CityDto>
            {
                Items = Mapper.Map<List<CityDto>>(result.Data),
                TotalItems = result.TotalItems,
                ErrorMessage = result.ErrorMessage
            };
        }

        public override async Task<CityDto> CreateAsync(CityDto dto, CancellationToken ct = default)
        {
            var created = await base.CreateAsync(dto, ct);
            await _cacheService.UpdateCacheAsync(ct);
            return created;
        }

        public override async Task UpdateAsync(int id, CityDto dto, CancellationToken ct = default)
        {
            await base.UpdateAsync(id, dto, ct);
            await _cacheService.UpdateCacheAsync(ct);
        }

        public override async Task DeleteAsync(int? id, CancellationToken ct = default)
        {
            if (id.HasValue)
            {
                await _cascadeDeleteService.DeleteCity(id.Value);
                await _cacheService.UpdateCacheAsync(ct);
                return;
            }
            await base.DeleteAsync(id, ct);
        }

        public async Task DeleteCityWithRelatedDataAsync(int id)
        {
            await _cascadeDeleteService.DeleteCity(id);
            await _cacheService.UpdateCacheAsync();
        }

        public async Task<bool> CityExistsAsync(int id)
        {
            return await Repository.FindFirstOrDefault(x => x.Id == id) != null;
        }

        public async Task<int> GetRelatedCompaniesCountAsync(int cityId)
        {
            var companyRepository = UnitOfWork.GetRepository<Company>();
            return await companyRepository.GetQueryable()
                .CountAsync(c => c.CityId == cityId && !c.IsDeleted);
        }
    }
}
