using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Application.ViewModels.OrderModel.Products
{
    public enum TaxCalculationType
    {
        SimplePercentage,
        SimpleFixed,
        SpecialFormula
    }
    public class OrderProductViewModel
    {
        public int? Id { get; set; }
        public int? ProductId { get; set; }
        public int? OrderId { get; set; }


        [Display(Name = "Наименование")]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Модель")]
        public string Model { get; set; } = string.Empty;

        [Display(Name = "Производитель")]
        public OrderManufacturerViewModel? Manufacturer { get; set; }

        [Display(Name = "Количество")]
        [Range(1, int.MaxValue, ErrorMessage = "Минимальное количество - 1")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Вес (кг)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Вес должен быть положительным")]
        public decimal Weight { get; set; }

        [Display(Name = "Цена (¥)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть положительной")]
        public decimal Price { get; set; }

        [Display(Name = "Дата доставки")]
        [DataType(DataType.Date)]
        public DateTime DeliveryDate { get; set; }

        [Display(Name = "Срок поставки")]
        public int LeadTime { get; set; }

        [Display(Name = "Комментарий ")]
        public string Comment { get; set; } = string.Empty;

        public decimal MarginRate => ProductTaxes.FirstOrDefault(t => t.Name == "Маржа")?.Value ?? 0;
        public decimal UnforeseenExpensesRate => ProductTaxes.FirstOrDefault(t => t.Name == "Не предвиденные расходы")?.Value ?? 0;


        public List<OrderTaxProductViewModel> ProductTaxes { get; set; } = new();
        public List<OrderCodeProductViewModel> Codes { get; set; } = new();


        public string? MetrologicalNumber { get; set; }
        public DateTime? MetrologicalExpiryDate { get; set; }
        public int? CodeTNVDId { get; set; }
        public string CodeTNVDName { get; set; } = string.Empty;
        public decimal CodeTNVDRate { get; set; }
        public List<SelectListItem> AvailableCodes { get; set; } = new();

        public decimal PriceInRub(decimal exchangeRate) => Price * exchangeRate;
        public decimal TotalPriceCNY => Price * Quantity;
        public decimal TotalPriceInRub(decimal exchangeRate) => PriceInRub(exchangeRate) * Quantity;
        public decimal CalculateBankCommission(decimal exchangeRate) => PriceInRub(exchangeRate) * (ProductTaxes.FirstOrDefault(t => t.Name == "Комиссия банка")?.Value ?? 0 / 100);
        public decimal PriceAfterBankCommission(decimal exchangeRate) => PriceInRub(exchangeRate) + CalculateBankCommission(exchangeRate);
        public decimal TotalPriceAfterBankCommission(decimal exchangeRate) => PriceAfterBankCommission(exchangeRate) * Quantity;


        public decimal CalculateProfit(decimal exchangeRate, decimal marginRate) => PriceAfterBankCommission(exchangeRate) * (marginRate / 100);
        public decimal PriceAfterProfit(decimal exchangeRate, decimal marginRate) => PriceAfterBankCommission(exchangeRate) + CalculateProfit(exchangeRate, marginRate);
        public decimal TotalPriceAfterProfit(decimal exchangeRate, decimal marginRate) => PriceAfterProfit(exchangeRate, marginRate) * Quantity;


        public decimal CalculateUnforeseenExpense(decimal exchangeRate, decimal unforeseenExpensesRate) => TotalPriceInRub(exchangeRate) * (unforeseenExpensesRate / 100);


        public decimal CalculateTNVD(decimal exchangeRate, decimal dutyRate) => TotalPriceInRub(exchangeRate) * (dutyRate / 100);
        public decimal CalculateUnexpectedCosts(decimal exchangeRate, decimal unexpectedCostsRate) => TotalPriceInRub(exchangeRate) * (unexpectedCostsRate / 100);
    }

    public class OrderManufacturerViewModel
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class OrderTaxProductViewModel
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string MeasureUnit { get; set; } = string.Empty;
        public TaxCalculationType CalculationType { get; set; }
        public string Formula { get; set; } = string.Empty;
        public bool IsCalculated { get; set; }
    }

    public static class TaxFormulas
    {
        public static decimal CalculateBankCommission(decimal priceInRubles, decimal commissionRate)
        {
            return priceInRubles * (commissionRate / 100);
        }
        public static decimal CalculateMargin(decimal priceAfterCommission, decimal marginRate)
        {
            return priceAfterCommission * (marginRate / 100);
        }
        public static decimal CalculateUnforeseenExpensesRate(decimal priceInRubles, decimal unforeseenExpensesRate)
        {
            return priceInRubles * (unforeseenExpensesRate / 100);
        }
    }

    public class OrderCodeProductViewModel
    {
        public string CodeName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }
}
