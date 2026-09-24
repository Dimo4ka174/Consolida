using Application.ViewModels.OrderModel.Products;
using Application.ViewModels.OrderModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using DB.Entity.Enum;
using DB.Entity;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.Excel;

namespace Application.DataAccessLayer.Service.ImportExcel
{
    /// <summary>
    /// Валидация данных из Excel. Загрузка связанных данных из БД, и создание продуктов, если они не были найдены в БД. Построение OrderFormViewModel.
    /// </summary>
    public class ExcelOrderProcessingService : IExcelOrderProcessingService
    {
        private readonly ICacheService<Manufacturer> _manufacturerCache;
        private readonly IExcelService _excelService;
        private readonly IUnitOfWork _unitOfWork;

        public ExcelOrderProcessingService(
            ICacheService<Manufacturer> manufacturerCacheService,
            IExcelService excelService,
            IUnitOfWork unitOfWork)
        {
            _manufacturerCache = manufacturerCacheService;
            _excelService = excelService;
            _unitOfWork = unitOfWork;
        }

        public async Task<OrderFormDto> ProcessExcelOrderAsync(IFormFile file)
        {
            var excelProducts = _excelService.ParseProductsFromExcel(file);
            ValidateExcelProducts(excelProducts);

            var existingProducts = await GetExistingProductsAsync(excelProducts);
            var newProducts = await CreateMissingProducts(excelProducts, existingProducts);

            var orderModel = new OrderFormDto
            {
                CreationDate = DateTime.Now,
                Priority = Priority.Medium,
                Status = Status.Registered,
                Products = MapToOrderProducts(excelProducts, existingProducts, newProducts)
            };

            orderModel.CalculateTotals();
            return orderModel;
        }

        /// <summary>
        /// Создание списка модели для продукции
        /// </summary>
        private List<OrderProductViewModel> MapToOrderProducts(List<ExcelProduct> excelProducts, List<Product> dbProducts, List<Product> newProducts)
        {
            // Единый ключ: Name + Model в нижнем регистре без пробелов по краям
            var allProducts = dbProducts
                .Concat(newProducts)
                .ToDictionary(p => (
                    Name: (p.Name ?? "").Trim().ToLower(),
                    Model: (p.Model ?? "").Trim().ToLower()
                ));

            return excelProducts.Select(ep =>
            {
                var key = (
                    Name: (ep.Name ?? "").Trim().ToLower(),
                    Model: (ep.Model ?? "").Trim().ToLower()
                );
                var product = allProducts[key];

                return new OrderProductViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Model = product.Model,
                    Manufacturer = product.Manufacturer != null
                        ? new OrderManufacturerViewModel
                        {
                            Id = product.Manufacturer.Id,
                            Name = product.Manufacturer.Name
                        }
                        : null,
                    Quantity = ep.Quantity,
                    Price = ep.Price,
                    DeliveryDate = ep.DeliveryDate,
                    LeadTime = ep.LeadTime,
                    Comment = ep.Comment ?? string.Empty
                };
            }).ToList();
        }

        /// <summary>
        /// Создание продукции и производителей которых не нашлось в БД при после парсинга excel документа
        /// </summary>
        private async Task<List<Product>> CreateMissingProducts(List<ExcelProduct> excelProducts, List<Product> existingProducts)
        {
            var newProducts = new List<Product>();

            // Актуальный кэш производителей
            await _manufacturerCache.UpdateCacheAsync();
            var manufacturersCache = (await _manufacturerCache.GetCachedDataAsync())
                .ToDictionary(m => m.Name, m => m);

            // Словарь существующих продуктов с нормализованным ключом
            var existingProductsDict = existingProducts.ToDictionary(
                p => (Name: (p.Name ?? "").Trim().ToLower(), Model: (p.Model ?? "").Trim().ToLower()),
                p => p);

            // Уникальные производители, которых нет в кэше
            var missingManufacturers = excelProducts
                .Where(p => !string.IsNullOrWhiteSpace(p.Manufacturer))
                .Select(p => p.Manufacturer!.Trim())
                .Distinct()
                .Where(name => !manufacturersCache.ContainsKey(name))
                .ToList();

            // Все операции создания оборачиваем в транзакцию
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (missingManufacturers.Any())
                {
                    var newManufacturers = missingManufacturers
                        .Select(name => new Manufacturer { Name = name })
                        .ToList();

                    await _unitOfWork.GetRepository<Manufacturer>().CreateRange(newManufacturers);
                    await _unitOfWork.SaveChangesAsync();
                    await _manufacturerCache.UpdateCacheAsync();

                    // Обновляем локальный словарь производителей
                    manufacturersCache = (await _manufacturerCache.GetCachedDataAsync())
                        .ToDictionary(m => m.Name, m => m);
                }

                // Отбираем действительно новые продукты
                foreach (var ep in excelProducts)
                {
                    var key = (Name: (ep.Name ?? "").Trim().ToLower(), Model: (ep.Model ?? "").Trim().ToLower());
                    if (existingProductsDict.ContainsKey(key))
                        continue;

                    Manufacturer? manufacturer = null;
                    if (!string.IsNullOrWhiteSpace(ep.Manufacturer))
                        manufacturersCache.TryGetValue(ep.Manufacturer.Trim(), out manufacturer);

                    newProducts.Add(new Product
                    {
                        Name = ep.Name,
                        Model = ep.Model,
                        Manufacturer = manufacturer,
                        Price = ep.Price,
                        CreatedDate = DateTime.Now
                    });
                }

                if (newProducts.Any())
                {
                    await _unitOfWork.GetRepository<Product>().CreateRange(newProducts);
                    await _unitOfWork.SaveChangesAsync();
                }
            });

            return newProducts;
        }

        /// <summary>
        /// Нахождение и получение продукции из БД в сопоставлении с продукцией из excel
        /// </summary>
        private async Task<List<Product>> GetExistingProductsAsync(List<ExcelProduct> excelProducts)
        {
            // Нормализованные ключи для поиска в БД
            var productKeys = excelProducts
                .Select(p => new
                {
                    Name = (p.Name ?? "").Trim().ToLower(),
                    Model = (p.Model ?? "").Trim().ToLower()
                })
                .Distinct()
                .ToList();

            // Загружаем все продукты с производителями
            var normalizedKeys = productKeys.Select(k => (k.Name + "|" + k.Model).ToLower()).ToList();
            var dbProducts = await _unitOfWork.GetRepository<Product>()
                .GetQueryable()
                .Include(p => p.Manufacturer)
                .Where(p => normalizedKeys.Contains((p.Name + "|" + p.Model).ToLower()))
                .AsNoTracking()
                .ToListAsync();

            var dbProductsDict = dbProducts.ToDictionary(
                p => (Name: (p.Name ?? "").Trim().ToLower(), Model: (p.Model ?? "").Trim().ToLower()));

            return productKeys
                .Where(key => dbProductsDict.ContainsKey((key.Name, key.Model)))
                .Select(key => dbProductsDict[(key.Name, key.Model)])
                .ToList();
        }

        /// <summary>
        /// Проверка валидации
        /// </summary>
        /// <param name="products">Список товаров из excel</param>
        private void ValidateExcelProducts(List<ExcelProduct> products)
        {
            if (products == null || !products.Any())
                throw new ArgumentException("Файл не содержит данных о товарах.");

            foreach (var product in products)
            {
                if (string.IsNullOrWhiteSpace(product.Name))
                    throw new ArgumentException("Наименование товара не может быть пустым.");

                if (product.Quantity <= 0)
                    throw new ArgumentException(
                        $"Количество товара '{product.Name}' должно быть больше 0.");

                if (product.Price <= 0)
                    throw new ArgumentException(
                        $"Цена товара '{product.Name}' должна быть больше 0.");
            }
        }
    }
}