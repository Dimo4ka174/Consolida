using System.Text.Json.Serialization;
using System.Globalization;

namespace Application.ViewModels.OrderModel
{
    public class ExportOrderViewModel
    {
        public int OrderId { get; set; }
        public int? StatusId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public decimal ExchangeRate { get; set; }
        public bool DownloadDocument { get; set; }
        public DateTime? DateCreationTKP { get; set; }
        public TkpSettings? TkpSettings { get; set; }

        //Информация о возможной оптимизации даты доставки
        [JsonIgnore]
        public string DeliveryOptimizationMessage { get; set; } = string.Empty;

        // Итоговые суммы
        public decimal TotalWithoutVat => Products.Sum(p => p.CalculatedTotals.AmountWithoutVAT);
        public decimal Vat => TotalWithVat - TotalWithoutVat;
        public decimal TotalWithVat => Products.Sum(p => p.CalculatedTotals.AmountVAT);

        public MetrologicalInfoViewModel? MetrologicalInfo { get; set; }
        public List<ProductData> Products { get; set; } = new List<ProductData>();
        public Dictionary<int, decimal> OrderTaxes { get; set; } = new Dictionary<int, decimal>();
    }

    public class ProductData
    {
        public int ProductId { get; set; }
        public int? CodeTNVDId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ManufacturerName { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int? LeadTime { get; set; }

        [JsonPropertyName("DeliveryDate")]
        public string? DeliveryDateString { get; set; }

        [JsonIgnore]
        public DateTime? DeliveryDate =>
            DateTime.TryParseExact(DeliveryDateString, "dd/MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : null;
        public string Comment { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Weight { get; set; }
        public decimal MarginRate { get; set; } = 0;
        public decimal UnforeseenExpensesRate { get; set; } = 0;

        public ProductCalculatedTotals CalculatedTotals { get; set; } = new ProductCalculatedTotals();

        public string? CodeTNVD { get; set; }
        public string? MetrologicalNumber { get; set; }
        public DateTime? MetrologicalExpiryDate { get; set; }

        public Dictionary<int, ProductTaxItem> ProductTaxes { get; set; } = new();

        public CalculationDetails Details { get; set; } = new CalculationDetails();
    }

    public class ProductCalculatedTotals
    {
        public decimal UnitPriceWithoutVAT { get; set; }
        public decimal AmountWithoutVAT { get; set; }
        public decimal UnitPriceVAT { get; set; }
        public decimal AmountVAT { get; set; }
    }

    public class CalculationDetails
    {
        public decimal PriceInRUB { get; set; }
        public decimal PriceTotalRUB { get; set; }
        public decimal BankCommissionPerUnit { get; set; }
        public decimal BankCommissionTotal { get; set; }
        public decimal MarginPerUnit { get; set; }
        public decimal MarginTotal { get; set; }
        public decimal DutyRate { get; set; }
        public decimal DutyTotal { get; set; }
        public decimal UnforeseenExpensesTotal { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal PriceWithoutVAT { get; set; }
        public decimal PriceWithVAT { get; set; }
    }

    public class ProductTaxItem
    {
        public decimal Value { get; set; }
        public bool IsManual { get; set; }
    }

    public class TkpSettings
    {
        public string TkpNumber { get; set; } = "";
        public string DeliveryTime { get; set; } = "недель с момента получения предоплаты";
        public string DeliveryTerms { get; set; } = "до склада Заказчика (стоимость доставки включена в стоимость товара)";
        public string PaymentTerms { get; set; } = "";
    }
}
