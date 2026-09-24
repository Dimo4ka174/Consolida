namespace Application.ViewModels.OrderModel.ExcelDoc
{
    public class ExportCalculationViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CalculationDate { get; set; } = DateTime.Now;


        // Основные параметры заказа
        public decimal ExchangeRate { get; set; }
        public decimal BankCommissionRate { get; set; }


        // Итоговые суммы
        public decimal TotalCostWithoutVAT { get; set; }
        public decimal TotalVAT { get; set; }
        public decimal TotalCostWithVAT { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalWeight { get; set; }


        // Налоги заказа
        public List<CalculationTaxViewModel> OrderTaxes { get; set; } = new();

        // Товары с детальным расчетом
        public List<CalculationProductViewModel> Products { get; set; } = new();

        // Дополнительная информация
        public string Comment { get; set; } = string.Empty;
        public string CalculatedBy { get; set; } = string.Empty;
    }

    public class CalculationTaxViewModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string MeasureUnit { get; set; } = string.Empty;
    }

    public class CalculationProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public int Quantity { get; set; }


        // Цены
        public decimal PriceInCNY { get; set; }
        public decimal PriceInRUB { get; set; }
        public decimal PriceTotalRUB { get; set; }


        // Банковская комиссия
        public decimal BankCommissionPerUnit { get; set; }
        public decimal BankCommissionTotal { get; set; }
        public decimal PriceWithBankCommission { get; set; }


        // Маржа
        public decimal MarginRate { get; set; }
        public decimal MarginPerUnit { get; set; }
        public decimal MarginTotal { get; set; }
        public decimal PriceAfterMargin { get; set; }


        // Код ТНВЭД
        public string? CodeTNVD { get; set; }
        public decimal DutyRate { get; set; }
        public decimal DutyTotal { get; set; }


        // Непредвиденные расходы
        public decimal UnforeseenExpensesRate { get; set; }
        public decimal UnforeseenExpensesTotal { get; set; }


        // Налоги на товар
        public List<CalculationTaxViewModel> ProductTaxes { get; set; } = new();


        // Итоговые суммы по товару
        public decimal TotalTaxes { get; set; }
        public decimal PriceWithoutVAT { get; set; }
        public decimal PriceWithVAT { get; set; }
        public decimal TotalWithoutVAT { get; set; }
        public decimal TotalWithVAT { get; set; }


        // Вес
        public decimal Weight { get; set; }
        public decimal WeightTotal { get; set; }


        // Комментарий
        public string Comment { get; set; } = string.Empty;
    }
}