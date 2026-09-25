using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Entity.Enum;
using DB.Abstract;

namespace DB.Entity
{
    [Table("ConsolidationPools")]
    public class ConsolidationPool : IEntity
    {
        [Key]
        public int? Id { get; set; }
        public string? LockedByUserId { get; set; }

        public int TargetWeek { get; set; }
        public decimal TotalWeight { get; set; }
        public string Color { get; set; } = "#e0e0e0";
        public Status Status { get; set; } = Status.Paid;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? LockedAt { get; set; }
        public uint Version { get; set; }
        public bool IsDeleted { get; set; }

        public List<Order> Orders { get; set; } = new();
    }
}
