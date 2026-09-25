using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Entity.Enum;
using DB.Abstract;

namespace DB.Entity
{
    [Table("ConsolidationPoolHistories")]
    public class ConsolidationPoolHistory : IEntity
    {
        [Key]
        public int? Id { get; set; }

        public int PoolId { get; set; }

        public ConsolidationPoolEventType EventType { get; set; }

        [Column(TypeName = "varchar(500)")]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "varchar(200)")]
        public string? OldValue { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? NewValue { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string ChangedBy { get; set; } = "System";

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }
    }
}