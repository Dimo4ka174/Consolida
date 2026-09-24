using Application.DataAccessLayer.Interface.OrderService;
using Application.DataAccessLayer.Interface.Common;
using Application.ViewModels.OrderModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using DB.Entity;

namespace Application.DataAccessLayer.Service.Entity
{
    public class OrderCreationService : IOrderCreationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderCreationService> _logger;

        public OrderCreationService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<OrderCreationService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OrderCreationResult> CreateOrderAsync(OrderFormDto model, string userName)
        {
            var result = new OrderCreationResult();

            try
            {
                // 1. Бизнес-валидация
                var validationErrors = await ValidateOrder(model);
                if (validationErrors.Any())
                {
                    result.Errors = validationErrors;
                    return result;
                }

                // Налоги заказа и продуктов
                var allTaxes = await _unitOfWork.GetRepository<TaxType>()
                    .GetQueryable()
                    .Where(t => !t.IsDeleted)
                    .ToListAsync();

                // 2. Создание заказа
                var order = await CreateOrderWithProducts(model, userName, allTaxes);
                result.OrderId = order.Id;
                result.Success = true;

                _logger.LogInformation("Order {OrderNumber} (ID: {OrderId}) created successfully by {User}",
                    order.OrderNumber, order.Id, userName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                result.Errors.Add($"Произошла ошибка при создании заказа: {ex.Message}");
            }

            return result;
        }

        private async Task<List<string>> ValidateOrder(OrderFormDto model)
        {
            var errors = new List<string>();

            if (!model.CompanyId.HasValue || model.CompanyId <= 0)
                errors.Add("Выберите компанию");
            if (!model.CustomerId.HasValue || model.CustomerId <= 0)
                errors.Add("Выберите клиента");
            if (model.Priority == default)
                errors.Add("Выберите приоритет");

            if (model.CompanyId.HasValue && model.CustomerId.HasValue)
            {
                var customer = await _unitOfWork.GetRepository<Customer>()
                    .GetQueryable()
                    .FirstOrDefaultAsync(c => c.Id == model.CustomerId.Value && c.CompanyId == model.CompanyId.Value);

                if (customer == null)
                    errors.Add("Выбранный клиент не принадлежит выбранной компании");
            }

            foreach (var product in model.Products)
            {
                if (string.IsNullOrEmpty(product.ProductName) || product.Quantity <= 0 || product.Price <= 0 || product.Weight <= 0)
                {
                    errors.Add("Проверьте данные о товарах: все поля должны быть заполнены.");
                    break;
                }
            }

            return errors;
        }

        private async Task<Order> CreateOrderWithProducts(OrderFormDto model, string userName, List<TaxType> allTaxes)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                model.CalculateTotals();
                model.CreationDate = DateTime.UtcNow;

                var order = _mapper.Map<Order>(model);
                order.LastChangeDate = DateTime.Now;
                order.Status = DB.Entity.Enum.Status.Registered;

                await _unitOfWork.GetRepository<Order>().Create(order);
                await _unitOfWork.SaveChangesAsync();

                // Создаём налоги на заказ
                foreach (var tax in allTaxes)
                {
                    var orderTax = new OrderTax
                    {
                        OrderId = order.Id,
                        TaxTypeId = tax.Id,
                        Cost = tax.Cost,
                    };
                    await _unitOfWork.GetRepository<OrderTax>().Create(orderTax);
                }

                // История статуса
                var historyStatus = new OrderStatusHistory
                {
                    OrderId = order.Id,
                    ChangeDate = DateTime.Now,
                    ChangedBy = userName,
                    NewStatus = DB.Entity.Enum.Status.Registered,
                    OldStatus = DB.Entity.Enum.Status.None,
                };
                await _unitOfWork.GetRepository<OrderStatusHistory>().Create(historyStatus);

                // Товары
                foreach (var productVm in model.Products)
                {
                    var orderProduct = new OrderProduct
                    {
                        ProductId = productVm.ProductId,
                        OrderId = order.Id,
                        Quantity = productVm.Quantity,
                        Price = productVm.Price,
                        DeliveryDate = productVm.DeliveryDate,
                        LeadTime = productVm.LeadTime,
                        Weight = productVm.Weight > 0 ? productVm.Weight : 0.1m,
                        TotalPrice = productVm.Quantity * productVm.Price,
                        Comment = productVm.Comment,
                    };

                    await _unitOfWork.GetRepository<OrderProduct>().Create(orderProduct);
                    await _unitOfWork.SaveChangesAsync();

                    // Налоги на товар
                    foreach (var tax in allTaxes)
                    {
                        var orderTaxProduct = new OrderTaxProduct
                        {
                            TaxTypeId = tax.Id,
                            OrderId = order.Id,
                            OrderProductId = orderProduct.Id,
                            Cost = tax.Cost,
                        };
                        await _unitOfWork.GetRepository<OrderTaxProduct>().Create(orderTaxProduct);
                    }
                }

                // Обновляем заказ
                order.LastChangeDate = DateTime.Now;
                _unitOfWork.GetRepository<Order>().Update(order);
                await _unitOfWork.SaveChangesAsync();

                return order;
            });
        }
    }
}
