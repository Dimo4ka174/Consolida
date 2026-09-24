using Application.ViewModels.OrderModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using AutoMapper;
using DB.Entity;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.OrderService;
using Application.DataAccessLayer.Interface.Excel;

namespace Application.DataAccessLayer.Service.OrderService
{
    public class OrderSaveService : IOrderSaveService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderSaveService> _logger;
        private readonly IExportExcelService _documentService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;

        public OrderSaveService(
            IUnitOfWork unitOfWork, 
            ILogger<OrderSaveService> logger, 
            IExportExcelService documentService, 
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _documentService = documentService;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
        }

        public async Task<OrderSaveResult> SaveOrderDataAsync(ExportOrderViewModel model)
        {
            if (model == null || model.Products == null || !model.Products.Any())
            {
                _logger.LogError("Model is null or Products is empty.");
                return new OrderSaveResult { Success = false, ErrorMessage = "Invalid data" };
            }

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var order = await _unitOfWork.GetRepository<Order>()
                        .GetById(model.OrderId);
                    if (order == null)
                    {
                        _logger.LogError($"Order with ID {model.OrderId} not found.");
                        return new OrderSaveResult { Success = false, ErrorMessage = "Order not found" };
                    }

                    // 2. Обновляем основные данные заказа
                    await UpdateOrderBasicData(order, model);
                    // 3. Обрабатываем товары заказа
                    await ProcessOrderProducts(order.Id, model.Products);
                    await _unitOfWork.SaveChangesAsync();

                    return new OrderSaveResult { Success = true, Order = order };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during saving order data");
                return new OrderSaveResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<Stream> GenerateTKPAsync(ExportOrderViewModel model, Order order)
        {
            var orderWithDetails = await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .Where(o => o.Id == order.Id)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.Company)
                .Include(o => o.OrdersProducts)
                    .ThenInclude(op => op.Product)
                    .ThenInclude(p => p.Manufacturer)
                .FirstOrDefaultAsync();

            foreach (var product in model.Products)
            {
                var orderProduct = orderWithDetails?.OrdersProducts.FirstOrDefault(op => op.ProductId == product.ProductId);
                if (orderProduct?.Product?.Manufacturer != null)
                {
                    product.ManufacturerName = orderProduct.Product.Manufacturer.Name ?? "Не указано";
                }
            }
            _logger.LogInformation("GenerateTKPAsync: Products data:");
            foreach (var p in model.Products)
            {
                _logger.LogInformation("Product {Id}: UnitPriceWithoutVAT={Unit}, AmountWithoutVAT={Amount}",
                    p.ProductId,
                    p.CalculatedTotals.UnitPriceWithoutVAT,
                    p.CalculatedTotals.AmountWithoutVAT);
            }


            return await _documentService.GenerateTKPAsync(model, orderWithDetails, model.TkpSettings);
        }

        #region Private Methods - Основная логика обработки заказа

        private async Task UpdateOrderBasicData(Order order, ExportOrderViewModel model)
        {
            _mapper.Map(model, order);

            if (model.DateCreationTKP.HasValue)
            {
                order.DateCreationTKP = model.DateCreationTKP.Value;
            }

            // Обрабатываем налоги заказа
            await UpdateOrderTax(order.Id, model.OrderTaxes);
            // Удаляем продукты, которые отсутствуют в новой версии заказа
            await RemoveMissingProducts(order.Id, model.Products.Select(p => p.ProductId).ToList());
        }

        #endregion

        #region Private Methods - Работа с налогами заказа

        private async Task UpdateOrderTax(int? orderId, Dictionary<int, decimal> orderTaxes)
        {
            var existingTaxes = await _unitOfWork.GetRepository<OrderTax>()
                .GetQueryable()
                .Where(ot => ot.OrderId == orderId)
                .ToListAsync();

            var taxTypes = await _unitOfWork.GetRepository<TaxType>()
                .GetQueryable()
                .ToDictionaryAsync(t => t.Id.Value);

            // Обновляем или добавляем налоги
            foreach (var tax in orderTaxes)
            {
                if (!taxTypes.ContainsKey(tax.Key)) continue;

                var existingTax = existingTaxes.FirstOrDefault(t => t.TaxTypeId == tax.Key);
                if (existingTax != null)
                {
                    existingTax.Cost = tax.Value;
                    existingTax.IsCalculated = true;
                    _unitOfWork.GetRepository<OrderTax>().Update(existingTax);
                }
                else
                {
                    var newTax = new OrderTax
                    {
                        OrderId = orderId,
                        TaxTypeId = tax.Key,
                        Cost = tax.Value,
                        IsCalculated = true
                    };
                    await _unitOfWork.GetRepository<OrderTax>().Create(newTax);
                }
            }
        }

        #endregion

        #region Private Methods - Работа с продуктами заказа

        private async Task ProcessOrderProducts(int? orderId, List<ProductData> products)
        {
            var existingOrderProducts = await _unitOfWork.GetRepository<OrderProduct>()
                .GetQueryable()
                .Where(op => op.OrderId == orderId)
                .Include(op => op.OrdersTaxes)
                .Include(op => op.Product)
                .ToListAsync();

            var productDictionary = existingOrderProducts.ToDictionary(op => op.ProductId.Value);
            decimal totalCost = 0;
            decimal totalWeight = 0;

            foreach (var productModel in products)
            { 
                // Создаем/Обновляем OrderProduct
                if (!productDictionary.TryGetValue(productModel.ProductId, out var orderProduct))
                {
                    orderProduct = await CreateNewOrderProduct(orderId, productModel);
                }
                else
                {
                    UpdateOrderProduct(orderProduct, productModel);
                }

                if (productModel.CodeTNVDId.HasValue && orderProduct.Product != null)
                {
                    var product = orderProduct.Product;
                    if (product.CodeTNVDId != productModel.CodeTNVDId)
                    {
                        product.CodeTNVDId = productModel.CodeTNVDId;
                        _unitOfWork.GetRepository<Product>().Update(product);
                    }
                }

                totalWeight += (productModel.Weight * productModel.Quantity);
                totalCost += productModel.Price * productModel.Quantity;

                // Обрабатываем коды ТНВЭД/налоги для продукта
                //await ProcessProductCodes(orderProduct.Id.Value, orderId.Value, productModel.CodeTNVD);
                await ProcessProductTaxes(orderProduct.Id.Value, productModel.ProductTaxes);
                await ProcessProductMetrologicalInfo(orderProduct.Id.Value, orderId.Value, productModel);
            }

            await UpdateOrderTotals(orderId.Value, totalCost, totalWeight);
        }

        private async Task UpdateOrderTotals(int orderId, decimal totalCost, decimal totalWeight)
        {
            var order = await _unitOfWork.GetRepository<Order>().GetById(orderId);
            if (order != null)
            {
                order.TotalCost = totalCost;
                order.TotalWeight = totalWeight;
                order.LastChangeDate = DateTime.Now;

                _unitOfWork.GetRepository<Order>().Update(order);
            }
        }

        private async Task<OrderProduct> CreateNewOrderProduct(int? orderId, ProductData productModel)
        {
            var product = await _unitOfWork.GetRepository<Product>()
                .GetById(productModel.ProductId);
            if (product == null) throw new Exception($"Product with ID {productModel.ProductId} not found");
            var orderProduct = _mapper.Map<OrderProduct>(productModel);

            await _unitOfWork.GetRepository<OrderProduct>().Create(orderProduct);
            return orderProduct;
        }

        private void UpdateOrderProduct(OrderProduct orderProduct, ProductData productModel)
        {
            orderProduct.Quantity = productModel.Quantity;
            orderProduct.Price = productModel.Price;
            orderProduct.Weight = productModel.Weight;
            orderProduct.LeadTime = productModel.LeadTime ?? 0;
            orderProduct.Comment = productModel.Comment;

            _unitOfWork.GetRepository<OrderProduct>().Update(orderProduct);
        }

        private async Task ProcessProductTaxes(int orderProductId, Dictionary<int, ProductTaxItem> productTaxes)
        {
            var existingTaxes = await _unitOfWork.GetRepository<OrderTaxProduct>()
                .GetQueryable()
                .Where(ot => ot.OrderProductId == orderProductId)
                .ToListAsync();

            var taxTypes = await _unitOfWork.GetRepository<TaxType>()
                .GetQueryable()
                .ToDictionaryAsync(t => t.Id.Value);

            foreach (var tax in productTaxes)
            {
                if (!taxTypes.ContainsKey(tax.Key)) continue;

                var item = tax.Value;
                var existingTax = existingTaxes.FirstOrDefault(t => t.TaxTypeId == tax.Key);

                if (existingTax != null)
                {
                    existingTax.Cost = item.Value;
                    existingTax.IsCalculated = !item.IsManual;
                    _unitOfWork.GetRepository<OrderTaxProduct>().Update(existingTax);
                }
                else
                {
                    var newTax = new OrderTaxProduct
                    {
                        OrderProductId = orderProductId,
                        TaxTypeId = tax.Key,
                        Cost = item.Value,
                        IsCalculated = !item.IsManual
                    };
                    await _unitOfWork.GetRepository<OrderTaxProduct>().Create(newTax);
                }
            }
        }

        private async Task RemoveMissingProducts(int? orderId, List<int> currentProductIds)
        {
            var existingProducts = await _unitOfWork.GetRepository<OrderProduct>()
                .GetQueryable()
                .Where(op => op.OrderId == orderId)
                .ToListAsync();

            var productsToRemove = existingProducts
                .Where(p => !currentProductIds.Contains(p.ProductId.Value))
                .ToList();

            foreach (var product in productsToRemove)
            {
                await _unitOfWork.GetRepository<OrderProduct>().Delete(product.Id);
            }
        }

        private async Task ProcessProductMetrologicalInfo(int orderProductId, int orderId, ProductData productModel)
        {
            var hasNumber = !string.IsNullOrWhiteSpace(productModel.MetrologicalNumber);
            var hasDate = productModel.MetrologicalExpiryDate.HasValue;
            var hasData = hasNumber || hasDate;

            var existing = await _unitOfWork.GetRepository<MetrologicalInfo>()
                .GetQueryable(includeDeleted: true)
                .FirstOrDefaultAsync(m => m.OrderProductId == orderProductId);

            if (!hasData)
            {
                if (existing != null && !existing.IsDeleted)
                {
                    existing.IsDeleted = true;
                    _unitOfWork.GetRepository<MetrologicalInfo>().Update(existing);
                }
                return;
            }

            if (existing != null)
            {
                existing.OrderProductId = orderProductId;
                existing.RegistrationNumber = productModel.MetrologicalNumber ?? string.Empty;
                existing.ExpiryDate = productModel.MetrologicalExpiryDate ?? DateTime.Now;
                existing.IsDeleted = false;
                _unitOfWork.GetRepository<MetrologicalInfo>().Update(existing);
            }
            else
            {
                var info = new MetrologicalInfo
                {
                    OrderProductId = orderProductId,
                    RegistrationNumber = productModel.MetrologicalNumber ?? string.Empty,
                    ExpiryDate = productModel.MetrologicalExpiryDate ?? DateTime.Now,
                    IsDeleted = false
                };
                await _unitOfWork.GetRepository<MetrologicalInfo>().Create(info);
            }
        }

        #endregion
    }
}