using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Abstract;

namespace DB.Entity
{
    [Table("OrderNotifications")]
    public class OrderNotification : IEntity
    {
        [Key]
        public int? Id { get; set; }
        public int? OrderId { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CompletedBy { get; set; }

        public bool IsDeleted { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}