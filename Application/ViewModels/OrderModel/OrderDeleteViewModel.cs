namespace Application.ViewModels.OrderModel
{
    public class DeleteOrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalWeight { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;

        public List<ProductSummary> Products { get; set; } = new();
        public List<StatusHistoryItem> StatusHistory { get; set; } = new();

        // Параметры возврата
        public int ReturnPage { get; set; } = 1;
        public string? SearchString { get; set; }
        public int? CustomerId { get; set; }
        public string? StatusFilter { get; set; }
        public string? SortOrder { get; set; }
    }

    public class ProductSummary
    {
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class StatusHistoryItem
    {
        public DateTime ChangeDate { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
    }
}