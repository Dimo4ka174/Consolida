using Application.DataAccessLayer.Interface.Common;
using DB.Abstract;
using DB.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.DataAccessLayer.Service.Common
{
    public class CascadeSoftDeleteService : ICascadeSoftDeleteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CascadeSoftDeleteService> _logger;
        private readonly ICacheService<City> _cityCacheService;
        private readonly ICacheService<Company> _companyCacheService;

        public CascadeSoftDeleteService(
            IUnitOfWork unitOfWork,
            ILogger<CascadeSoftDeleteService> logger,
            ICacheService<City> cityCacheService,
            ICacheService<Company> companyCacheService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cityCacheService = cityCacheService;
            _companyCacheService = companyCacheService;
        }

        public async Task DeleteCity(int cityId, CancellationToken ct = default)
        {
            if (cityId <= 0) throw new ArgumentException("Invalid City ID");
            _logger.LogInformation("Deleting City ID: {CityId}", cityId);

            if (_unitOfWork.HasActiveTransaction)
                await DeleteCities(new List<int?> { cityId }, ct);
            else
                await _unitOfWork.ExecuteInTransactionAsync(
                    () => DeleteCities(new List<int?> { cityId }, ct), ct);
        }

        private async Task DeleteCities(IEnumerable<int?> cityIds, CancellationToken ct)
        {
            var idsList = cityIds.Where(id => id != null && id > 0).Select(id => id.Value).ToList();
            if (!idsList.Any()) return;

            await MarkAsDeleted(_unitOfWork.GetRepository<City>().GetQueryable(),
                c => idsList.Contains(c.Id.Value), ct);

            var companyIds = await _unitOfWork.GetRepository<Company>().GetQueryable()
                .Where(c => c.CityId.HasValue && idsList.Contains(c.CityId.Value))
                .Select(c => c.Id)
                .ToListAsync(ct);

            if (companyIds.Any())
                await DeleteCompanies(companyIds, ct);

            await _cityCacheService.UpdateCacheAsync();
        }

        public async Task DeleteCompany(int companyId, CancellationToken ct = default)
        {
            if (companyId <= 0) throw new ArgumentException("Invalid Company ID");
            _logger.LogInformation("Deleting Company ID: {CompanyId}", companyId);

            if (_unitOfWork.HasActiveTransaction)
                await DeleteCompanies(new List<int?> { companyId }, ct);
            else
                await _unitOfWork.ExecuteInTransactionAsync(
                    () => DeleteCompanies(new List<int?> { companyId }, ct), ct);
        }

        private async Task DeleteCompanies(IEnumerable<int?> companyIds, CancellationToken ct)
        {
            var idsList = companyIds.Where(id => id != null && id > 0).Select(id => id.Value).ToList();
            if (!idsList.Any()) return;

            await MarkAsDeleted(_unitOfWork.GetRepository<Company>().GetQueryable(),
                c => idsList.Contains(c.Id.Value), ct);

            var customerIds = await _unitOfWork.GetRepository<Customer>().GetQueryable()
                .Where(c => c.CompanyId.HasValue && idsList.Contains(c.CompanyId.Value))
                .Select(c => c.Id)
                .ToListAsync(ct);

            if (customerIds.Any())
                await DeleteCustomers(customerIds, ct);

            await _companyCacheService.UpdateCacheAsync();
        }

        public async Task DeleteCustomer(int customerId, CancellationToken ct = default)
        {
            if (customerId <= 0) throw new ArgumentException("Invalid Customer ID");
            _logger.LogInformation("Deleting Customer ID: {CustomerId}", customerId);

            if (_unitOfWork.HasActiveTransaction)
                await DeleteCustomers(new List<int?> { customerId }, ct);
            else
                await _unitOfWork.ExecuteInTransactionAsync(
                    () => DeleteCustomers(new List<int?> { customerId }, ct), ct);
        }

        private async Task DeleteCustomers(IEnumerable<int?> customerIds, CancellationToken ct)
        {
            var idsList = customerIds.Where(id => id != null && id > 0).Select(id => id.Value).ToList();
            if (!idsList.Any()) return;

            await MarkAsDeleted(_unitOfWork.GetRepository<Customer>().GetQueryable(),
                c => idsList.Contains(c.Id.Value), ct);
        }

        private async Task MarkAsDeleted<T>(IQueryable<T> query,
            Expression<Func<T, bool>> predicate, CancellationToken ct)
            where T : class, IEntity
        {
            await query.Where(predicate)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true), ct);
        }
    }
}
