using Application.ViewModels.OrderModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using DB.Entity.Enum;
using AutoMapper;
using DB.Entity;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.OrderService;

namespace Application.DataAccessLayer.Service.OrderService
{
    public class OrderDuplicateService : IOrderDuplicateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderDuplicateService> _logger;
        private readonly IOrderNumberGenerator _orderNumberGenerator;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderDuplicateService(
            IUnitOfWork unitOfWork,
            ILogger<OrderDuplicateService> logger,
            IOrderNumberGenerator orderNumberGenerator,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _orderNumberGenerator = orderNumberGenerator;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OrderDuplicateResult> DuplicateOrderAsync(DuplicateOrderViewModel model)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // 1. Получаем все существующие TaxType IDs для проверки
                    var existingTaxTypeIds = await _unitOfWork.GetRepository<TaxType>()
                        .GetQueryable()
                        .Where(tt => !tt.IsDeleted)
                        .Select(tt => tt.Id.Value)
                        .ToHashSetAsync();

                    // 2. Получаем оригинальный заказ для копирования некоторых данных
                    var originalOrder = await _unitOfWork.GetRepository<Order>()
                        .GetQueryable()
                        .Include(o => o.Customer)
                            .ThenInclude(c => c.Company)
                        .FirstOrDefaultAsync(o => o.Id == model.OriginalOrderId);

                    if (originalOrder == null)
                    {
                        return new OrderDuplicateResult
                        {
                            Success = false,
                            ErrorMessage = $"Оригинальный заказ с ID {model.OriginalOrderId} не найден"
                        };
                    }

                    // 3. Генерируем новый номер заказа
                    var newOrderNumber = await _orderNumberGenerator.GenerateDuplicateOrderNumber(originalOrder.OrderNumber);

                    // 4. Создаем новый заказ
                    var newOrder = new Order
                    {
                        OrderNumber = newOrderNumber,
                        CustomerId = originalOrder.CustomerId,
                        Priority = originalOrder.Priority,
                        Status = Status.Registered,
                        CreationDate = DateTime.Now,
                        LastChangeDate = DateTime.Now,
                        ExchangeRate = model.ExchangeRate,
                        Comment = model.Comment,
                        TotalWeight = 0,
                        TotalCost = 0,
                        IsDeleted = false
                    };

                    await _unitOfWork.GetRepository<Order>().Create(newOrder);
                    await _unitOfWork.SaveChangesAsync();

                    // 5. Копируем налоги заказа
                    await DuplicateOrderTaxes(newOrder.Id.Value, model.OrderTaxes, existingTaxTypeIds);

                    // 6. Копируем продукты и их налоги
                    decimal totalWeight = 0;
                    decimal totalCost = 0;
                    var taxTypeIds = await GetTaxTypeIdsAsync();

                    foreach (var productModel in model.Products)
                    {
                        var orderProduct = new OrderProduct
                        {
                            OrderId = newOrder.Id,
                            ProductId = productModel.ProductId,
                            Quantity = productModel.Quantity,
                            Price = productModel.Price,
                            Weight = productModel.Weight > 0 ? productModel.Weight : 0.1m,
                            DeliveryDate = productModel.DeliveryDate ?? DateTime.Now,
                            LeadTime = productModel.LeadTime,
                            Comment = productModel.Comment,
                            TotalPrice = productModel.Price * productModel.Quantity
                        };

                        await _unitOfWork.GetRepository<OrderProduct>().Create(orderProduct);
                        await _unitOfWork.SaveChangesAsync();

                        // Обновляем веса и стоимости
                        totalWeight += productModel.Weight * productModel.Quantity;
                        totalCost += productModel.Price * productModel.Quantity;

                        // 7. Обрабатываем код ТНВЭД для продукта
                        await ProcessProductCode(orderProduct.Id.Value, newOrder.Id.Value, productModel);

                        // 8. Сохраняем спец доп расходы (маржа, непредвиденные расходы)
                        await SaveSpecialProductTaxes(orderProduct.Id.Value, newOrder.Id.Value, productModel, taxTypeIds, existingTaxTypeIds);

                        // 9. Копируем налоги продукта
                        await DuplicateProductTaxes(orderProduct.Id.Value, newOrder.Id.Value, productModel.ProductTaxes, existingTaxTypeIds);
                    }

                    // 10. Обновляем итоговые суммы заказа
                    newOrder.TotalWeight = totalWeight;
                    newOrder.TotalCost = totalCost;
                    _unitOfWork.GetRepository<Order>().Update(newOrder);

                    // 11. Создаем запись в истории статусов
                    var historyStatus = new OrderStatusHistory
                    {
                        OrderId = newOrder.Id.Value,
                        ChangeDate = DateTime.Now,
                        ChangedBy = GetCurrentUserName(),
                        NewStatus = Status.Registered,
                        OldStatus = Status.None,
                    };
                    await _unitOfWork.GetRepository<OrderStatusHistory>().Create(historyStatus);
                    await _unitOfWork.SaveChangesAsync();

                    return new OrderDuplicateResult
                    {
                        Success = true,
                        NewOrderId = newOrder.Id,
                        NewOrderNumber = newOrder.OrderNumber,
                        NewOrder = newOrder
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error duplicating order {OriginalOrderId}", model.OriginalOrderId);
                return new OrderDuplicateResult
                {
                    Success = false,
                    ErrorMessage = $"Ошибка при дублировании заказа: {ex.Message}"
                };
            }
        }

        private async Task DuplicateOrderTaxes(int newOrderId, Dictionary<int, decimal> orderTaxes, HashSet<int> existingTaxTypeIds)
        {
            foreach (var tax in orderTaxes)
            {
                if (existingTaxTypeIds.Contains(tax.Key))
                {
                    var orderTax = new OrderTax
                    {
                        OrderId = newOrderId,
                        TaxTypeId = tax.Key,
                        Cost = tax.Value,
                        IsCalculated = true,
                        IsDeleted = false
                    };
                    await _unitOfWork.GetRepository<OrderTax>().Create(orderTax);
                    _logger.LogInformation($"Created OrderTax: TaxTypeId={tax.Key}, Cost={tax.Value}");
                }
                else
                {
                    _logger.LogWarning($"Skipping OrderTax with TaxTypeId={tax.Key} - not found in TaxType table");
                }
            }
        }

        private async Task SaveSpecialProductTaxes(int orderProductId, int orderId, DuplicateProductData productModel, Dictionary<string, int> taxTypeIds, HashSet<int> existingTaxTypeIds)
        {
            // Сохраняем Маржу (MarginRate)
            if (taxTypeIds.TryGetValue("Маржа", out var marginTaxTypeId) &&
                existingTaxTypeIds.Contains(marginTaxTypeId))
            {
                var marginTax = new OrderTaxProduct
                {
                    OrderProductId = orderProductId,
                    OrderId = orderId,
                    TaxTypeId = marginTaxTypeId,
                    Cost = productModel.MarginRate,
                    IsCalculated = true,
                    IsDeleted = false
                };
                await _unitOfWork.GetRepository<OrderTaxProduct>().Create(marginTax);
                _logger.LogInformation($"Created Margin Tax: TaxTypeId={marginTaxTypeId}, Cost={productModel.MarginRate}");
            }

            // Сохраняем Непредвиденные расходы (UnforeseenExpensesRate)
            if (taxTypeIds.TryGetValue("Не предвиденные расходы", out var unforeseenTaxTypeId) &&
                existingTaxTypeIds.Contains(unforeseenTaxTypeId))
            {
                var unforeseenTax = new OrderTaxProduct
                {
                    OrderProductId = orderProductId,
                    OrderId = orderId,
                    TaxTypeId = unforeseenTaxTypeId,
                    Cost = productModel.UnforeseenExpensesRate,
                    IsCalculated = true,
                    IsDeleted = false
                };
                await _unitOfWork.GetRepository<OrderTaxProduct>().Create(unforeseenTax);
                _logger.LogInformation($"Created Unforeseen Tax: TaxTypeId={unforeseenTaxTypeId}, Cost={productModel.UnforeseenExpensesRate}");
            }
        }

        private async Task<Dictionary<string, int>> GetTaxTypeIdsAsync()
        {
            var taxTypes = await _unitOfWork.GetRepository<TaxType>()
                .GetQueryable()
                .Where(tt => !tt.IsDeleted)
                .ToListAsync();

            return taxTypes.ToDictionary(t => t.Name, t => t.Id.Value);
        }

        private async Task ProcessProductCode(int orderProductId, int orderId, DuplicateProductData productModel)
        {
            if (string.IsNullOrEmpty(productModel.CodeTNVD)) return;

            // Ищем существующий код ТНВЭД
            var codeTNVD = await _unitOfWork.GetRepository<CodeTNVD>()
                .FindFirstOrDefault(c => c.Name == productModel.CodeTNVD);

            // Если код не найден и есть ставка, создаем новый
            if (codeTNVD == null && productModel.DutyRate > 0)
            {
                codeTNVD = new CodeTNVD
                {
                    Name = productModel.CodeTNVD,
                    Rate = productModel.DutyRate,
                    IsDeleted = false
                };
                await _unitOfWork.GetRepository<CodeTNVD>().Create(codeTNVD);
                await _unitOfWork.SaveChangesAsync();
            }

            if (codeTNVD != null)
            {
                // Обновляем продукт, если нужно
                var product = await _unitOfWork.GetRepository<Product>()
                    .GetQueryable()
                    .FirstOrDefaultAsync(p => p.Id == productModel.ProductId);

                if (product != null && product.CodeTNVDId != codeTNVD.Id)
                {
                    product.CodeTNVDId = codeTNVD.Id;
                    _unitOfWork.GetRepository<Product>().Update(product);
                }
            }
        }

        private async Task DuplicateProductTaxes(int orderProductId, int orderId, Dictionary<int, decimal> productTaxes, HashSet<int> existingTaxTypeIds)
        {
            foreach (var tax in productTaxes)
            {
                // Проверяем, существует ли TaxTypeId
                if (existingTaxTypeIds.Contains(tax.Key))
                {
                    var productTax = new OrderTaxProduct
                    {
                        OrderProductId = orderProductId,
                        OrderId = orderId,
                        TaxTypeId = tax.Key,
                        Cost = tax.Value,
                        IsCalculated = true,
                        IsDeleted = false
                    };
                    await _unitOfWork.GetRepository<OrderTaxProduct>().Create(productTax);
                    _logger.LogInformation($"Created OrderTaxProduct: TaxTypeId={tax.Key}, Cost={tax.Value}");
                }
                else
                {
                    _logger.LogWarning($"Skipping OrderTaxProduct with TaxTypeId={tax.Key} - not found in TaxType table");
                }
            }
        }

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
        }
    }
}