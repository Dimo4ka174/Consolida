namespace Application.ViewModels.OrderModel.Products
{
    public class ExcelProduct
    {
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int LeadTime { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}

