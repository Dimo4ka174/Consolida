using System.Text.Json.Serialization;
using System.Globalization;
using DB.Entity;

namespace Application.ViewModels.OrderModel
{
    public class DuplicateOrderViewModel
    {
        public int OriginalOrderId { get; set; }
        public decimal ExchangeRate { get; set; }
        public string Comment { get; set; } = string.Empty;

        public List<DuplicateProductData> Products { get; set; } = new List<DuplicateProductData>();
        public Dictionary<int, decimal> OrderTaxes { get; set; } = new Dictionary<int, decimal>();
    }

    public class DuplicateProductData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("DeliveryDateString")]
        public string? DeliveryDateString { get; set; }

        [JsonIgnore]
        public DateTime? DeliveryDate =>
            DateTime.TryParseExact(DeliveryDateString, "dd/MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : null;

        public int LeadTime { get; set; }
        public string Comment { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Weight { get; set; }
        public decimal MarginRate { get; set; }
        public decimal UnforeseenExpensesRate { get; set; }
        public string? CodeTNVD { get; set; }
        public decimal DutyRate { get; set; }

        // Налоги продукта (ключ - TaxTypeId, значение - ставка/сумма)
        public Dictionary<int, decimal> ProductTaxes { get; set; } = new Dictionary<int, decimal>();
    }

    public class OrderDuplicateResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int? NewOrderId { get; set; }
        public string? NewOrderNumber { get; set; }
        public Order? NewOrder { get; set; }
    }
}