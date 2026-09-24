using System.Linq.Expressions;
using Application.DataAccessLayer.Interface.Common;
using DB.Abstract;
using DB.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.DataAccessLayer.Service.Common
{
    public class CascadeSoftDeleteService : ICascadeSoftDeleteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CascadeSoftDeleteService> _logger;
        private readonly ICacheService<City> _cityCacheService;
        private readonly ICacheService<Company> _companyCacheService;
        private readonly ICacheService<Manufacturer> _manufacturerCacheService;
        private readonly ICacheService<MeasureUnit> _measureUnitCacheService;
        private readonly ICacheService<TaxType> _taxTypeCacheService;

        public CascadeSoftDeleteService(
            IUnitOfWork unitOfWork,
            ILogger<CascadeSoftDeleteService> logger,
            ICacheService<City> cityCacheService,
            ICacheService<Company> companyCacheService,
            ICacheService<Manufacturer> manufacturerCacheService,
            ICacheService<MeasureUnit> measureUnitCacheService,
            ICacheService<TaxType> taxTypeCacheService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cityCacheService = cityCacheService;
            _companyCacheService = companyCacheService;
            _manufacturerCacheService = manufacturerCacheService;
            _measureUnitCacheService = measureUnitCacheService;
            _taxTypeCacheService = taxTypeCacheService;
        }

        // -------------------- City --------------------

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

        // -------------------- Company --------------------

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

        // -------------------- Customer --------------------

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

        // -------------------- Manufacturer --------------------

        public async Task DeleteManufacturer(int manufacturerId, CancellationToken ct = default)
        {
            if (manufacturerId <= 0) throw new ArgumentException("Invalid Manufacturer ID");
            _logger.LogInformation("Deleting Manufacturer ID: {ManufacturerId}", manufacturerId);

            if (_unitOfWork.HasActiveTransaction)
                await DeleteManufacturers(new List<int?> { manufacturerId }, ct);
            else
                await _unitOfWork.ExecuteInTransactionAsync(
                    () => DeleteManufacturers(new List<int?> { manufacturerId }, ct), ct);
        }

        private async Task DeleteManufacturers(IEnumerable<int?> manufacturerIds, CancellationToken ct)
        {
            var idsList = manufacturerIds.Where(id => id != null && id > 0).Select(id => id.Value).ToList();
            if (!idsList.Any()) return;

            await MarkAsDeleted(_unitOfWork.GetRepository<Manufacturer>().GetQueryable(),
                m => idsList.Contains(m.Id.Value), ct);

            var productIds = await _unitOfWork.GetRepository<Product>().GetQueryable()
                .Where(p => p.ManufacturerId.HasValue && idsList.Contains(p.ManufacturerId.Value))
                .Select(p => p.Id)
                .ToListAsync(ct);

            if (productIds.Any())
                await DeleteProducts(productIds, ct);

            await _manufacturerCacheService.UpdateCacheAsync();
        }

        // -------------------- CodeTNVD --------------------

        public async Task DeleteCodeTNVD(int codeTnvdId, CancellationToken ct = default)
        {
            if (codeTnvdId <= 0) throw new ArgumentException("Invalid CodeTNVD ID");
            _logger.LogInformation("Deleting CodeTNVD ID: {CodeTNVDId}", codeTnvdId);

            if (_unitOfWork.HasActiveTransaction)
                await DeleteCodesTNVD(new List<int?> { codeTnvdId }, ct);
            else
                await _unitOfWork.ExecuteInTransactionAsync(
                    () => DeleteCodesTNVD(new List<int?> { codeTnvdId }, ct), ct);
        }

        private async Task DeleteCodesTNVD(IEnumerable<int?> codeIds, CancellationToken ct)
        {
            var idsList = codeIds.Where(id => id != null && id > 0).Select(id => id.Value).ToList();
            if (!idsList.Any()) return;

            await MarkAsDeleted(_unitOfWork.GetRepository<CodeTNVD>().GetQueryable(),
                c => idsList.Contains(c.Id.Value), ct);

            var productIds = await _unitOfWork.GetRepository<Product>().GetQueryable()
                .Where(p => p.CodeTNVDId.HasValue && idsList.Contains(p.CodeTNVDId.Value))
                .Select(p => p.Id)
                .ToListAsync(ct);

            if (productIds.Any())
                await DeleteProducts(productIds, ct);
        }

        // -------------------- Product --------------------

        public async Task DeleteProduct(int productId, CancellationToken ct = default)
        {
            if (productId <= 0) throw new ArgumentException("Invalid Product ID");
            _logger.LogInformation("Deleting Product ID: {ProductId}", productId);

            if (_unitOfWork.HasActiveTransaction)
                await DeleteProducts(new List<int?> { productId }, ct);
            else
                await _unitOfWork.ExecuteInTransactionAsync(
                    () => DeleteProducts(new List<int?> { productId }, ct), ct);
        }

        private async Task DeleteProducts(IEnumerable<int?> productIds, CancellationToken ct)
        {
            var idsList = productIds.Where(id => id != null && id > 0).Select(id => id.Value).ToList();
            if (!idsList.Any()) return;

            await MarkAsDeleted(_unitOfWork.GetRepository<Product>().GetQueryable(),
                p => idsList.Contains(p.Id.Value), ct);

            // TODO (коммит Order): каскадно помечать OrderProduct.
        }

        // -------------------- MeasureUnit --------------------

        public async Task DeleteMeasureUnit(int measureUnitId, CancellationToken ct = default)
        {
            if (measureUnitId <= 0) throw new ArgumentException("Invalid MeasureUnit ID");
            _logger.LogInformation("Deleting MeasureUnit ID: {MeasureUnitId}", measureUnitId);

            if (_unitOfWork.HasActiveTransaction)
                await DeleteMeasureUnits(new List<int?> { measureUnitId }, ct);
            else
                await _unitOfWork.ExecuteInTransactionAsync(
                    () => DeleteMeasureUnits(new List<int?> { measureUnitId }, ct), ct);
        }

        private async Task DeleteMeasureUnits(IEnumerable<int?> measureUnitIds, CancellationToken ct)
        {
            var idsList = measureUnitIds.Where(id => id != null && id > 0).Select(id => id.Value).ToList();
            if (!idsList.Any()) return;

            await MarkAsDeleted(_unitOfWork.GetRepository<MeasureUnit>().GetQueryable(),
                m => idsList.Contains(m.Id.Value), ct);

            var taxTypeIds = await _unitOfWork.GetRepository<TaxType>().GetQueryable()
                .Where(t => t.MeasureUnitId.HasValue && idsList.Contains(t.MeasureUnitId.Value))
                .Select(t => t.Id)
                .ToListAsync(ct);

            if (taxTypeIds.Any())
                await DeleteTaxTypes(taxTypeIds, ct);

            await _measureUnitCacheService.UpdateCacheAsync();
        }

        // -------------------- TaxType --------------------

        public async Task DeleteTaxType(int taxTypeId, CancellationToken ct = default)
        {
            if (taxTypeId <= 0) throw new ArgumentException("Invalid TaxType ID");
            _logger.LogInformation("Deleting TaxType ID: {TaxTypeId}", taxTypeId);

            if (_unitOfWork.HasActiveTransaction)
                await DeleteTaxTypes(new List<int?> { taxTypeId }, ct);
            else
                await _unitOfWork.ExecuteInTransactionAsync(
                    () => DeleteTaxTypes(new List<int?> { taxTypeId }, ct), ct);
        }

        private async Task DeleteTaxTypes(IEnumerable<int?> taxTypeIds, CancellationToken ct)
        {
            var idsList = taxTypeIds.Where(id => id != null && id > 0).Select(id => id.Value).ToList();
            if (!idsList.Any()) return;

            await MarkAsDeleted(_unitOfWork.GetRepository<TaxType>().GetQueryable(),
                t => idsList.Contains(t.Id.Value), ct);

            // OrderTaxProduct, привязанные к этому налогу, тоже помечаем удалёнными
            await MarkAsDeleted(_unitOfWork.GetRepository<OrderTaxProduct>().GetQueryable(),
                otp => otp.TaxTypeId.HasValue && idsList.Contains(otp.TaxTypeId.Value), ct);

            // OrderTax, привязанные к этому налогу
            await MarkAsDeleted(_unitOfWork.GetRepository<OrderTax>().GetQueryable(),
                ot => ot.TaxTypeId.HasValue && idsList.Contains(ot.TaxTypeId.Value), ct);

            await _taxTypeCacheService.UpdateCacheAsync();
        }

        // -------------------- Order --------------------

        public async Task DeleteOrder(int orderId, CancellationToken ct = default)
        {
            if (orderId <= 0) throw new ArgumentException("Invalid Order ID");
            _logger.LogInformation("Deleting Order ID: {OrderId}", orderId);

            if (_unitOfWork.HasActiveTransaction)
                await DeleteOrders(new List<int?> { orderId }, ct);
            else
                await _unitOfWork.ExecuteInTransactionAsync(
                    () => DeleteOrders(new List<int?> { orderId }, ct), ct);
        }

        private async Task DeleteOrders(IEnumerable<int?> orderIds, CancellationToken ct)
        {
            var idsList = orderIds.Where(id => id != null && id > 0).Select(id => id.Value).ToList();
            if (!idsList.Any()) return;

            // 1. Собираем ID продуктов заказа (понадобится для каскада на MetrologicalInfo)
            var orderProductIds = await _unitOfWork.GetRepository<OrderProduct>().GetQueryable()
                .Where(op => op.OrderId.HasValue && idsList.Contains(op.OrderId.Value))
                .Select(op => op.Id)
                .ToListAsync(ct);

            // 2. Мягко удаляем сам заказ
            await MarkAsDeleted(_unitOfWork.GetRepository<Order>().GetQueryable(),
                o => idsList.Contains(o.Id.Value), ct);

            // 3. Каскад: OrderProduct
            if (orderProductIds.Any())
            {
                await MarkAsDeleted(_unitOfWork.GetRepository<OrderProduct>().GetQueryable(),
                    op => orderProductIds.Contains(op.Id.Value), ct);

                // MetrologicalInfo, привязанные к OrderProduct
                await MarkAsDeleted(_unitOfWork.GetRepository<MetrologicalInfo>().GetQueryable(),
                    mi => mi.OrderProductId.HasValue && orderProductIds.Contains(mi.OrderProductId.Value), ct);
            }

            // 4. Каскад: OrderTax
            await MarkAsDeleted(_unitOfWork.GetRepository<OrderTax>().GetQueryable(),
                ot => ot.OrderId.HasValue && idsList.Contains(ot.OrderId.Value), ct);

            // 5. Каскад: OrderTaxProduct
            await MarkAsDeleted(_unitOfWork.GetRepository<OrderTaxProduct>().GetQueryable(),
                otp => otp.OrderId.HasValue && idsList.Contains(otp.OrderId.Value), ct);

            // 6. Каскад: OrderStatusHistory
            await MarkAsDeleted(_unitOfWork.GetRepository<OrderStatusHistory>().GetQueryable(),
                osh => osh.OrderId.HasValue && idsList.Contains(osh.OrderId.Value), ct);

            // 7. Каскад: MetrologicalInfo, привязанные к самому заказу
            await MarkAsDeleted(_unitOfWork.GetRepository<MetrologicalInfo>().GetQueryable(),
                mi => mi.OrderId.HasValue && idsList.Contains(mi.OrderId.Value), ct);
        }

        // -------------------- Helper --------------------

        private async Task MarkAsDeleted<T>(IQueryable<T> query,
            Expression<Func<T, bool>> predicate, CancellationToken ct)
            where T : class, IEntity
        {
            await query.Where(predicate)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true), ct);
        }
    }
}
