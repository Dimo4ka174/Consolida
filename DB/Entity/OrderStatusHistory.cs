using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Abstract;
using DB.Entity.Enum;

namespace DB.Entity
{
    [Table("OrderStatusHistory")]
    public class OrderStatusHistory : IEntity
    {
        [Key]
        public int? Id { get; set; }
        public int? OrderId { get; set; }
        
        
        public Status OldStatus { get; set; }
        public Status NewStatus { get; set; }
        public DateTime ChangeDate { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }


        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}
