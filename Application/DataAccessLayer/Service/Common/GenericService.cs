using Application.DataAccessLayer.Interface.Common;
using AutoMapper;
using DB.Abstract;

namespace Application.DataAccessLayer.Service.Common
{
    public abstract class GenericService<TEntity, TDto> : IGenericService<TEntity, TDto>
        where TEntity : class, IEntity
        where TDto : class
    {
        protected readonly IUnitOfWork UnitOfWork;
        protected readonly IMapper Mapper;
        protected readonly IFilterService<TEntity> FilterService;
        protected readonly ICacheService<TEntity>? Cache;

        protected GenericService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFilterService<TEntity> filterService,
            ICacheService<TEntity>? cache = null)
        {
            UnitOfWork = unitOfWork;
            Mapper = mapper;
            FilterService = filterService;
            Cache = cache;
        }

        protected IRepository<TEntity> Repository => UnitOfWork.GetRepository<TEntity>();

        protected virtual IQueryable<TEntity> GetPagedQuery() => Repository.GetQueryable();

        public virtual async Task<List<TDto>> GetAllAsync(CancellationToken ct = default)
        {
            var entities = Cache != null
                ? (await Cache.GetCachedDataAsync(ct)).ToList()
                : (await Repository.GetList()).ToList();

            return await MapToDtoListAsync(entities, ct);
        }

        public virtual async Task<TDto?> GetByIdAsync(int? id, CancellationToken ct = default)
        {
            var entity = await Repository.GetById(id);
            if (entity == null)
                return null;

            var dto = Mapper.Map<TDto>(entity);
            await AfterMapAsync(dto, entity, ct);
            return dto;
        }

        public virtual async Task<TDto> CreateAsync(TDto dto, CancellationToken ct = default)
        {
            var entity = Mapper.Map<TEntity>(dto);
            await Repository.Create(entity);
            await UnitOfWork.SaveChangesAsync(ct);
            await AfterCreateAsync(entity, dto, ct);
            await RefreshCacheAsync(ct);
            return Mapper.Map<TDto>(entity);
        }

        public virtual async Task UpdateAsync(int id, TDto dto, CancellationToken ct = default)
        {
            var entity = await Repository.GetById(id);
            if (entity == null)
                throw new KeyNotFoundException();

            Mapper.Map(dto, entity);
            Repository.Update(entity);
            await UnitOfWork.SaveChangesAsync(ct);
            await RefreshCacheAsync(ct);
        }

        public virtual async Task DeleteAsync(int? id, CancellationToken ct = default)
        {
            await Repository.Delete(id);
            await UnitOfWork.SaveChangesAsync(ct);
            await RefreshCacheAsync(ct);
        }

        public virtual async Task<bool> ExistsAsync(int? id, CancellationToken ct = default)
        {
            return await Repository.FindFirstOrDefault(x => x.Id == id) != null;
        }

        public virtual async Task<PagedResult<TDto>> GetPagedAsync(FilterParams parameters, CancellationToken ct = default)
        {
            FilterResult<TEntity> result;
            if (Cache != null)
            {
                var cached = await Cache.GetCachedDataAsync(ct);
                result = FilterService.ApplyFilters(cached, parameters);
            }
            else
            {
                result = FilterService.ApplyFilters(GetPagedQuery(), parameters);
            }

            return new PagedResult<TDto>
            {
                Items = await MapToDtoListAsync(result.Data, ct),
                TotalItems = result.TotalItems,
                ErrorMessage = result.ErrorMessage
            };
        }

        protected virtual Task<List<TDto>> MapToDtoListAsync(List<TEntity> entities, CancellationToken ct)
        {
            return Task.FromResult(Mapper.Map<List<TDto>>(entities));
        }

        protected virtual Task AfterMapAsync(TDto dto, TEntity entity, CancellationToken ct) => Task.CompletedTask;

        protected virtual Task AfterCreateAsync(TEntity entity, TDto dto, CancellationToken ct) => Task.CompletedTask;

        protected virtual async Task RefreshCacheAsync(CancellationToken ct)
        {
            if (Cache != null)
                await Cache.UpdateCacheAsync(ct);
        }

        protected static FilterParams CreateNameFilter(int page, int pageSize, string? searchString, string? sortOrder, string searchProperty = "Name")
        {
            return new FilterParams
            {
                Page = page,
                PageSize = pageSize,
                SearchString = searchString,
                SearchProperty = searchProperty,
                SortProperty = sortOrder?.Replace("_desc", ""),
                SortDirection = sortOrder?.EndsWith("_desc") ?? false ? "desc" : "asc"
            };
        }
    }
}
