using Application.DataAccessLayer.Interface.OrderService;
using Application.DataAccessLayer.Interface.Common;
using Application.ViewModels.OrderModel.Products;
using Microsoft.AspNetCore.Mvc.Rendering;
using Application.ViewModels.OrderModel;
using Microsoft.EntityFrameworkCore;
using DB.Entity.Enum;
using DB.Entity;

namespace Application.DataAccessLayer.Service.OrderService
{
    public class OrderDetailsService : IOrderDetailsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrencyCacheService _currencyCacheService;

        public OrderDetailsService(IUnitOfWork unitOfWork, ICurrencyCacheService currencyCacheService)
        {
            _unitOfWork = unitOfWork;
            _currencyCacheService = currencyCacheService;
        }

        public async Task<OrderDetailsViewModel?> GetOrderDetailsAsync(int orderId)
        {
            var order = await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .Include(o => o.Customer)
                .Include(o => o.MetrologicalInfo)
                .Include(o => o.OrderTaxes)
                    .ThenInclude(ot => ot.TaxType)
                    .ThenInclude(tt => tt.MeasureUnit)
                .Include(o => o.OrdersProducts)
                    .ThenInclude(op => op.Product)
                    .ThenInclude(p => p.Manufacturer)
                .Include(o => o.OrdersProducts)
                    .ThenInclude(op => op.OrdersTaxes)
                    .ThenInclude(ot => ot.TaxType)
                .Include(o => o.OrdersProducts)
                    .ThenInclude(op => op.Product)
                    .ThenInclude(p => p.CodeTNVD)
                .Include(o => o.OrdersProducts)
                    .ThenInclude(op => op.MetrologicalInfo)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return null;

            var exchangeRate = order.ExchangeRate > 0
                ? order.ExchangeRate
                : await _currencyCacheService.GetExchangeRateAsync();

            // Вычисляем максимальный LeadTime среди позиций
            var maxLeadTime = order.OrdersProducts?
                .Where(op => !op.IsDeleted)
                .Select(op => op.LeadTime)
                .DefaultIfEmpty(0)
                .Max();


            // История статусов
            var lastStatusChange = await _unitOfWork.GetRepository<OrderStatusHistory>()
                .GetQueryable()
                .Where(h => h.OrderId == orderId)
                .OrderByDescending(h => h.ChangeDate)
                .FirstOrDefaultAsync();

            var viewModel = new OrderDetailsViewModel
            {
                Id = order.Id ?? 0,
                OrderNumber = order.OrderNumber,
                CustomerName = order.Customer?.LastName + " " + order.Customer?.FirstName ?? "Не указан",
                OrderCreationDate = order.CreationDate,
                LastChangeData = order.LastChangeDate,
                DateCreationTKP = order.DateCreationTKP,
                Status = order.Status,
                TotalWeight = order.TotalWeight,
                TotalCost = order.TotalCost,
                TotalLeadTimeWeeks = maxLeadTime,
                PricePerKg = order.TotalWeight > 0 ? Math.Round(order.TotalCost / order.TotalWeight, 2) : 0,
                PricePerCubicMeter = 0,
                ExchangeRate = exchangeRate,
                Comment = order.Comment ?? "",
                IsSaved = order.OrderTaxes?.Any(ot => ot.IsCalculated) ?? false,
                SelectedStatusId = (int)order.Status,

                LastStatusChangeDate = lastStatusChange?.ChangeDate ?? DateTime.MinValue,
                LastStatusChangedBy = lastStatusChange?.ChangedBy ?? "System"
            };

            // Налоги заказа
            viewModel.OrderTaxes = order.OrderTaxes?
                .Where(ot => !ot.IsDeleted)
                .Select(ot => new OrderTaxViewModel
                {
                    Id = ot.TaxTypeId ?? 0,
                    Name = ot.TaxType?.Name ?? "Без названия",
                    Cost = ot.IsCalculated ? ot.Cost : (ot.TaxType?.Cost ?? 0),
                    MeasureUnit = ot.TaxType?.MeasureUnit?.Name ?? "₽",
                    IsCalculated = ot.IsCalculated
                }).ToList() ?? new List<OrderTaxViewModel>();

            // Товары
            viewModel.Products = order.OrdersProducts?
                .Where(op => !op.IsDeleted)
                .Select(op => new OrderProductViewModel
                {
                    ProductId = op.ProductId,
                    OrderId = op.OrderId,
                    ProductName = op.Product?.Name ?? "Не указано",
                    Model = op.Product?.Model ?? "",
                    Manufacturer = op.Product?.Manufacturer != null ? new OrderManufacturerViewModel
                    {
                        Id = op.Product.Manufacturer.Id,
                        Name = op.Product.Manufacturer.Name
                    } : null,
                    Quantity = op.Quantity,
                    Price = op.Price,
                    Weight = op.Weight > 0 ? op.Weight : 0.1m,
                    DeliveryDate = op.DeliveryDate,
                    LeadTime = op.LeadTime,
                    Comment = op.Comment,
                    CodeTNVDId = op.Product?.CodeTNVDId,
                    CodeTNVDName = op.Product?.CodeTNVD?.Name ?? "",
                    CodeTNVDRate = op.Product?.CodeTNVD?.Rate ?? 0,
                    MetrologicalNumber = op.MetrologicalInfo != null && !op.MetrologicalInfo.IsDeleted ? op.MetrologicalInfo.RegistrationNumber : null,
                    MetrologicalExpiryDate = op.MetrologicalInfo != null && !op.MetrologicalInfo.IsDeleted ? op.MetrologicalInfo.ExpiryDate : null,
                    ProductTaxes = op.OrdersTaxes?
                        .Where(ot => !ot.IsDeleted)
                        .Select(ot => new OrderTaxProductViewModel
                        {
                            Id = ot.Id,
                            Name = ot.TaxType?.Name ?? "Не указано",
                            Value = ot.Cost,
                            MeasureUnit = ot.TaxType?.MeasureUnit?.Name ?? "₽",
                            CalculationType = ot.TaxType?.MeasureUnit?.Name == "%"
                                ? TaxCalculationType.SimplePercentage
                                : TaxCalculationType.SimpleFixed,
                            IsCalculated = ot.IsCalculated
                        }).ToList() ?? new List<OrderTaxProductViewModel>()
                }).ToList() ?? new List<OrderProductViewModel>();

            // Доступные статусы
            viewModel.AvailableStatuses = Enum.GetValues(typeof(Status))
                .Cast<Status>()
                .Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = s.GetDisplayName(),
                    Selected = s == order.Status
                }).ToList();

            // Идентификаторы типов налогов
            var allTaxTypes = await _unitOfWork.GetRepository<TaxType>()
                .GetQueryable()
                .ToListAsync();
            viewModel.TaxTypeIds = allTaxTypes.ToDictionary(t => t.Name, t => t.Id!.Value);

            viewModel.MetrologicalInfo = order.MetrologicalInfo != null 
               ? new MetrologicalInfoViewModel
                {
                    RegistrationNumber = order.MetrologicalInfo.RegistrationNumber,
                    ExpiryDate = order.MetrologicalInfo.ExpiryDate
                } 
               : null;

            return viewModel;
        }
    }
}
