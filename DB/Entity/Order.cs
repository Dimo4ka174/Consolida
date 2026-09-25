using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Entity.Enum;
using DB.Abstract;

namespace DB.Entity
{
    [Table("Orders")]
    public class Order : IEntity
    {
        [Key]
        public int? Id { get; set; }
        public int? CustomerId { get; set; }
        public int? ConsolidationPoolId { get; set; }
        public string? LockedByUserId { get; set; }

        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public DateTime LastChangeDate { get; set; }
        public DateTime DateCreationTKP { get; set; }
        public DateTime LastStatusChangeDate { get; set; }
        public DateTime? LockedAt { get; set; }
        public Priority Priority { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalCost { get; set; }
        public string? Comment { get; set; }
        public Status Status { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("ConsolidationPoolId")]
        public virtual ConsolidationPool? ConsolidationPool { get; set; }

        public virtual MetrologicalInfo? MetrologicalInfo { get; set; }
        public virtual List<OrderTaxProduct> OrderTaxProduct { get; set; } = new List<OrderTaxProduct>();
        public virtual List<OrderTax> OrderTaxes { get; set; } = new List<OrderTax>();
        public virtual List<OrderProduct> OrdersProducts { get; set; } = new List<OrderProduct>();
        public virtual List<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
        public virtual List<OrderNotification> Notifications { get; set; } = new List<OrderNotification>();
    }
}
