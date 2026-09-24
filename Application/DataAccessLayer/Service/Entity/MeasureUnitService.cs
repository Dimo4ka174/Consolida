using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.MeasureUnitModel;
using AutoMapper;
using DB.Entity;

namespace Application.DataAccessLayer.Service.Entity
{
    public class MeasureUnitService : GenericService<MeasureUnit, MeasureUnitDto>, IMeasureUnitService
    {
        private readonly ICascadeSoftDeleteService _cascadeDeleteService;

        public MeasureUnitService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFilterService<MeasureUnit> filterService,
            ICacheService<MeasureUnit> cacheService,
            ICascadeSoftDeleteService cascadeDeleteService)
            : base(unitOfWork, mapper, filterService, cacheService)
        {
            _cascadeDeleteService = cascadeDeleteService;
        }

        public override async Task<PagedResult<MeasureUnitDto>> GetPagedAsync(FilterParams parameters, CancellationToken ct = default)
        {
            var cached = await Cache!.GetCachedDataAsync(ct);
            var result = FilterService.ApplyFilters(cached, parameters);
            return new PagedResult<MeasureUnitDto>
            {
                Items = Mapper.Map<List<MeasureUnitDto>>(result.Data),
                TotalItems = result.TotalItems,
                ErrorMessage = result.ErrorMessage
            };
        }

        // Переопределяем удаление для мягкого (каскадного) удаления
        public override async Task DeleteAsync(int? id, CancellationToken ct = default)
        {
            if (id.HasValue)
            {
                await _cascadeDeleteService.DeleteMeasureUnit(id.Value);
                await RefreshCacheAsync(ct);
            }
            else
            {
                await base.DeleteAsync(id, ct);
            }
        }
    }
}