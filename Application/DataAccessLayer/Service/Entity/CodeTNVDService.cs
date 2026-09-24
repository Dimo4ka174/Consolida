using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.CodeTNVDModel;
using AutoMapper;
using DB.Entity;

namespace Application.DataAccessLayer.Service.Entity
{
    public class CodeTNVDService : GenericService<CodeTNVD, CodeTNVDdto>, ICodeTNVDService
    {
        private readonly ICascadeSoftDeleteService _cascadeDeleteService;

        public CodeTNVDService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFilterService<CodeTNVD> filterService,
            ICascadeSoftDeleteService cascadeDeleteService)
            : base(unitOfWork, mapper, filterService)
        {
            _cascadeDeleteService = cascadeDeleteService;
        }

        public override Task<PagedResult<CodeTNVDdto>> GetPagedAsync(FilterParams parameters, CancellationToken ct = default)
        {
            var result = FilterService.ApplyFilters(GetPagedQuery(), parameters);
            return Task.FromResult(new PagedResult<CodeTNVDdto>
            {
                Items = Mapper.Map<List<CodeTNVDdto>>(result.Data),
                TotalItems = result.TotalItems,
                ErrorMessage = result.ErrorMessage
            });
        }

        // Переопределяем DeleteAsync для каскадного удаления (мягкое удаление)
        public override async Task DeleteAsync(int? id, CancellationToken ct = default)
        {
            if (id.HasValue)
            {
                await _cascadeDeleteService.DeleteCodeTNVD(id.Value);
                await RefreshCacheAsync(ct);
            }
            else
            {
                await base.DeleteAsync(id, ct);
            }
        }
    }
}