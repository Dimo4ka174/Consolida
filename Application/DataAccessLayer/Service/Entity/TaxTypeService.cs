using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.TaxTypeModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using AutoMapper;
using DB.Entity;

namespace Application.DataAccessLayer.Service.Entity
{
    public class TaxTypeService : GenericService<TaxType, TaxTypeDto>, ITaxTypeService
    {
        private readonly ICacheService<MeasureUnit> _measureUnitCacheService;
        private readonly ICascadeSoftDeleteService _cascadeDeleteService;

        public TaxTypeService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFilterService<TaxType> filterService,
            ICacheService<TaxType> taxTypeCacheService,
            ICacheService<MeasureUnit> measureUnitCacheService,
            ICascadeSoftDeleteService cascadeDeleteService)
            : base(unitOfWork, mapper, filterService, taxTypeCacheService)
        {
            _measureUnitCacheService = measureUnitCacheService;
            _cascadeDeleteService = cascadeDeleteService;
        }

        // Переопределяем маппинг списка, чтобы заполнить MeasureUnitsList
        protected override async Task<List<TaxTypeDto>> MapToDtoListAsync(List<TaxType> entities, CancellationToken ct)
        {
            var measureUnits = await _measureUnitCacheService.GetCachedDataAsync(ct);
            var muSelectList = measureUnits.Select(mu => new SelectListItem
            {
                Text = mu.Name,
                Value = mu.Id.ToString()
            }).ToList();

            var models = Mapper.Map<List<TaxTypeDto>>(entities);
            foreach (var model in models)
            {
                model.MeasureUnitsList = muSelectList;
            }
            return models;
        }

        protected override IQueryable<TaxType> GetPagedQuery()
        {
            return UnitOfWork.GetRepository<TaxType>().GetQueryableWithIncludes(t => t.MeasureUnit);
        }

        public override async Task<PagedResult<TaxTypeDto>> GetPagedAsync(FilterParams parameters, CancellationToken ct = default)
        {
            var result = FilterService.ApplyFilters(GetPagedQuery(), parameters);
            return new PagedResult<TaxTypeDto>
            {
                Items = await MapToDtoListAsync(result.Data, ct),
                TotalItems = result.TotalItems,
                ErrorMessage = result.ErrorMessage
            };
        }

        public async Task<TaxTypeDto> GetCreateModelAsync()
        {
            var measureUnits = await _measureUnitCacheService.GetCachedDataAsync();
            return new TaxTypeDto
            {
                MeasureUnitsList = measureUnits.Select(mu => new SelectListItem
                {
                    Text = mu.Name,
                    Value = mu.Id.ToString()
                }).ToList()
            };
        }

        public async Task<TaxTypeDto> GetEditModelAsync(int id)
        {
            var taxType = await Repository.GetById(id);
            if (taxType == null) return null;

            var measureUnits = await _measureUnitCacheService.GetCachedDataAsync();
            var model = Mapper.Map<TaxTypeDto>(taxType);
            model.MeasureUnitsList = measureUnits.Select(mu => new SelectListItem
            {
                Text = mu.Name,
                Value = mu.Id.ToString()
            }).ToList();
            return model;
        }

        // Переопределяем удаление для каскадного мягкого удаления
        public override async Task DeleteAsync(int? id, CancellationToken ct = default)
        {
            if (id.HasValue)
            {
                await _cascadeDeleteService.DeleteTaxType(id.Value);
                await RefreshCacheAsync(ct);
            }
            else
            {
                await base.DeleteAsync(id, ct);
            }
        }
    }
}