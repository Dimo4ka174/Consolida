using DB.Entity.Enum;

namespace Application.ViewModels.OrderModel
{
    public class OrderListDto
    {
        public int? Id { get; set; }
        public int? CustomerId { get; set; }
        public int? CompanyId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal TotalWeight { get; set; }
        public decimal TotalCost { get; set; }
        public Priority Priority { get; set; }
        public Status Status { get; set; }
        public DateTime LastChangeDate { get; set; }
    }
}
