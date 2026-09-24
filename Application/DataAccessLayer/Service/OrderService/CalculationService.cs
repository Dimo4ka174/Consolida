using Application.DataAccessLayer.Interface.CalculationService;
using Application.ViewModels.OrderModel.ExcelDoc;
using Application.ViewModels.OrderModel;
using Microsoft.EntityFrameworkCore;
using DB.Entity;
using Application.DataAccessLayer.Interface.Common;

namespace Application.DataAccessLayer.Service.OrderService
{
    public class CalculationService : ICalculationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrencyCacheService _currencyCacheService;

        public CalculationService(IUnitOfWork unitOfWork, ICurrencyCacheService currencyCacheService)
        {
            _unitOfWork = unitOfWork;
            _currencyCacheService = currencyCacheService;
        }

        public async Task<ExportCalculationViewModel> BuildCalculationViewModelAsync(ExportOrderViewModel model, string currentUser)
        {
            // Получаем заказ с дополнительными данными
            var order = await GetOrderWithDetailsAsync(model.OrderId);
            var taxTypeIds = await GetTaxTypeIdsAsync();

            // Собираем основную модель
            var calculation = new ExportCalculationViewModel
            {
                OrderId = model.OrderId,
                OrderNumber = model.OrderNumber ?? order?.OrderNumber,
                ExchangeRate = model.ExchangeRate > 0 ? model.ExchangeRate :
                              order?.ExchangeRate ?? await _currencyCacheService.GetExchangeRateAsync(),
                Comment = model.Comment,
                CalculatedBy = currentUser,
                CalculationDate = DateTime.Now
            };

            // Собираем налоги заказа (только непроцентные налоги и комиссию банка)
            await PopulateOrderTaxes(calculation, model, order, taxTypeIds);

            // Устанавливаем банковскую комиссию
            calculation.BankCommissionRate = GetBankCommissionRate(calculation, model, taxTypeIds);

            // Собираем данные по товарам
            await PopulateProducts(calculation, model, order);

            // Рассчитываем итоги
            CalculateTotals(calculation);

            return calculation;
        }

        private async Task<Order> GetOrderWithDetailsAsync(int orderId)
        {
            return await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .Include(o => o.OrderTaxes)
                    .ThenInclude(ot => ot.TaxType)
                .Include(o => o.OrdersProducts)
                    .ThenInclude(op => op.OrdersTaxes)
                        .ThenInclude(ot => ot.TaxType)
                .Include(o => o.OrdersProducts)
                    .ThenInclude(op => op.Product)
                .Include(o => o.OrdersProducts)
                    .ThenInclude(op => op.Product)
                        .ThenInclude(oc => oc.CodeTNVD)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        private async Task<Dictionary<string, int>> GetTaxTypeIdsAsync()
        {
            var taxTypes = await _unitOfWork.GetRepository<TaxType>()
                .GetQueryable()
                .ToListAsync();

            return taxTypes.ToDictionary(t => t.Name, t => t.Id.Value);
        }

        private async Task PopulateOrderTaxes(
            ExportCalculationViewModel calculation,
            ExportOrderViewModel model,
            Order order,
            Dictionary<string, int> taxTypeIds)
        {
            calculation.OrderTaxes = new List<CalculationTaxViewModel>();

            // Добавляем только комиссию банка (Маржу и Непредвиденные расходы НЕ добавляем - они на уровне товаров)
            var bankCommissionRate = GetTaxRate(model, order, taxTypeIds, "Комиссия банка");
            if (bankCommissionRate > 0)
            {
                calculation.OrderTaxes.Add(new CalculationTaxViewModel
                {
                    Name = "Комиссия банка",
                    Cost = bankCommissionRate,
                    MeasureUnit = "%"
                });
            }

            // Добавляем остальные непроцентные налоги заказа
            if (order?.OrderTaxes != null)
            {
                foreach (var tax in order.OrderTaxes)
                {
                    if (tax.TaxType != null)
                    {
                        var taxName = tax.TaxType.Name;

                        // Пропускаем процентные налоги (кроме комиссии банка)
                        if (taxName == "Маржа" || taxName == "Не предвиденные расходы")
                            continue;

                        // Проверяем, не добавили ли уже этот налог
                        if (!calculation.OrderTaxes.Any(t => t.Name == taxName))
                        {
                            // Добавляем только непроцентные налоги
                            var isPercentage = tax.TaxType.MeasureUnit?.Name == "%";
                            if (!isPercentage)
                            {
                                calculation.OrderTaxes.Add(new CalculationTaxViewModel
                                {
                                    Name = taxName,
                                    Cost = tax.Cost,
                                    MeasureUnit = tax.TaxType.MeasureUnit?.Name ?? "₽"
                                });
                            }
                        }
                    }
                }
            }
        }

        private decimal GetTaxRate(
            ExportOrderViewModel model,
            Order order,
            Dictionary<string, int> taxTypeIds,
            string taxName)
        {
            if (!taxTypeIds.ContainsKey(taxName))
                return 0;

            var taxTypeId = taxTypeIds[taxName];

            // Сначала из модели (текущие изменения пользователя)
            if (model.OrderTaxes != null && model.OrderTaxes.ContainsKey(taxTypeId))
            {
                return model.OrderTaxes[taxTypeId];
            }

            // Затем из сохраненного заказа
            if (order?.OrderTaxes != null)
            {
                var tax = order.OrderTaxes.FirstOrDefault(ot => ot.TaxTypeId == taxTypeId);
                if (tax != null)
                {
                    return tax.Cost;
                }
            }

            return 0;
        }

        private decimal GetBankCommissionRate(ExportCalculationViewModel calculation, ExportOrderViewModel model, Dictionary<string, int> taxTypeIds)
        {
            if (taxTypeIds.TryGetValue("Комиссия банка", out var commId))
            {
                if (model.OrderTaxes != null && model.OrderTaxes.TryGetValue(commId, out var rate))
                    return rate;
            }
            // fallback
            var bankTax = calculation.OrderTaxes?.FirstOrDefault(t => t.Name == "Комиссия банка");
            return bankTax?.Cost ?? 0;
        }

        private async Task PopulateProducts(
            ExportCalculationViewModel calculation,
            ExportOrderViewModel model,
            Order order)
        {
            calculation.Products = new List<CalculationProductViewModel>();

            foreach (var productModel in model.Products)
            {
                var product = await BuildProductCalculationAsync(productModel, calculation, order);
                calculation.Products.Add(product);
            }
        }

        private async Task<CalculationProductViewModel> BuildProductCalculationAsync(
            ProductData productModel,
            ExportCalculationViewModel calculation,
            Order order)
        {
            var product = new CalculationProductViewModel
            {
                ProductId = productModel.ProductId,
                ProductName = productModel.ProductName,
                Model = productModel.Model,
                Quantity = productModel.Quantity,
                PriceInCNY = productModel.Price,
                Weight = productModel.Weight,
                CodeTNVD = productModel.CodeTNVD,
                Comment = productModel.Comment,
                MarginRate = productModel.MarginRate,
                UnforeseenExpensesRate = productModel.UnforeseenExpensesRate
            };

            // Базовые расчеты
            product.PriceInRUB = product.PriceInCNY * calculation.ExchangeRate;
            product.PriceTotalRUB = product.PriceInRUB * product.Quantity;
            product.WeightTotal = product.Weight * product.Quantity;

            // Банковская комиссия
            product.BankCommissionPerUnit = product.PriceInRUB * (calculation.BankCommissionRate / 100);
            product.BankCommissionTotal = product.BankCommissionPerUnit * product.Quantity;
            product.PriceWithBankCommission = product.PriceInRUB + product.BankCommissionPerUnit;

            // Маржа
            product.MarginPerUnit = product.PriceInRUB * (product.MarginRate / 100);
            product.MarginTotal = product.MarginPerUnit * product.Quantity;
            product.PriceAfterMargin = product.MarginTotal + product.PriceTotalRUB;

            // Пошлина (если есть код ТНВЭД)
            if (!string.IsNullOrEmpty(product.CodeTNVD))
            {
                var code = await GetCodeTNVDAsync(product.CodeTNVD);
                if (code != null)
                {
                    product.DutyRate = code.Rate;
                    product.DutyTotal = product.PriceTotalRUB * (code.Rate / 100);
                }
            }

            // Непредвиденные расходы
            product.UnforeseenExpensesTotal = product.PriceTotalRUB * (product.UnforeseenExpensesRate / 100);

            // Налоги на товар (исключая процентные и уже учтенные налоги)
            product.ProductTaxes = await GetProductTaxesAsync(productModel.ProductId, order?.Id, product.Quantity);

            // TotalTaxes должен содержать сумму только тех налогов, которые в ProductTaxes
            product.TotalTaxes = product.ProductTaxes.Sum(t => t.Cost);

            // Итоговые суммы
            product.TotalWithoutVAT = product.PriceTotalRUB +
                                      product.BankCommissionTotal +
                                      product.MarginTotal +
                                      product.DutyTotal +
                                      product.UnforeseenExpensesTotal +
                                      product.TotalTaxes;

            product.TotalWithVAT = product.TotalWithoutVAT * 1.22m;

            // Цены за единицу
            product.PriceWithoutVAT = product.Quantity > 0 ? product.TotalWithoutVAT / product.Quantity : 0;
            product.PriceWithVAT = product.Quantity > 0 ? product.TotalWithVAT / product.Quantity : 0;

            return product;
        }

        private async Task<CodeTNVD> GetCodeTNVDAsync(string codeName)
        {
            return await _unitOfWork.GetRepository<CodeTNVD>()
                .GetQueryable()
                .FirstOrDefaultAsync(c => c.Name == codeName);
        }

        private async Task<List<CalculationTaxViewModel>> GetProductTaxesAsync(int productId, int? orderId, int quantity)
        {
            var taxes = new List<CalculationTaxViewModel>();

            if (orderId.HasValue)
            {
                var productTaxes = await _unitOfWork.GetRepository<OrderTaxProduct>()
                    .GetQueryable()
                    .Where(ot => ot.OrderProduct.OrderId == orderId &&
                                ot.OrderProduct.ProductId == productId)
                    .Include(ot => ot.TaxType)
                        .ThenInclude(tt => tt.MeasureUnit)
                    .ToListAsync();

                foreach (var tax in productTaxes)
                {
                    var taxName = tax.TaxType?.Name ?? "Неизвестный налог";
                    var measureUnit = tax.TaxType?.MeasureUnit?.Name ?? "₽";

                    // Исключаем процентные налоги и налоги, которые уже учтены отдельно
                    var excludedTaxes = new List<string>
                    {
                        "Комиссия банка",
                        "Маржа",
                        "Не предвиденные расходы",
                        "Пошлина"
                    };

                    if (excludedTaxes.Contains(taxName, StringComparer.OrdinalIgnoreCase))
                        continue;

                    // Также исключаем налоги с процентной единицей измерения
                    if (measureUnit == "%")
                        continue;

                    taxes.Add(new CalculationTaxViewModel
                    {
                        Name = taxName,
                        Cost = tax.Cost * quantity,
                        MeasureUnit = measureUnit
                    });
                }
            }

            return taxes;
        }

        private void CalculateTotals(ExportCalculationViewModel calculation)
        {
            calculation.TotalCostWithoutVAT = calculation.Products.Sum(p => p.TotalWithoutVAT);
            calculation.TotalCostWithVAT = calculation.Products.Sum(p => p.TotalWithVAT);
            calculation.TotalVAT = calculation.TotalCostWithVAT - calculation.TotalCostWithoutVAT;
            calculation.TotalProfit = calculation.Products.Sum(p => p.MarginTotal);
            calculation.TotalWeight = calculation.Products.Sum(p => p.WeightTotal);
        }
    }
}