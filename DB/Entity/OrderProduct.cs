using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Abstract;

namespace DB.Entity
{
    [Table("OrdersProducts")]
    public class OrderProduct : IEntity
    {
        [Key]
        public int? Id { get; set; }
        public int? OrderId { get; set; }
        public int? ProductId { get; set; }


        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Weight { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int LeadTime { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }


        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public virtual MetrologicalInfo? MetrologicalInfo { get; set; }

        public List<OrderTaxProduct> OrdersTaxes { get; set; } = new List<OrderTaxProduct>();
    }
}
