namespace Application.ViewModels.ProductModel
{
    public class ProductSearchResultDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int? ManufacturerId { get; set; }
        public string? ManufacturerName { get; set; }
    }

    public class CreateProductApiRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int? ManufacturerId { get; set; }
        public decimal Price { get; set; }
    }

    public class CreateManufacturerApiRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class ManufacturerLookupDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
