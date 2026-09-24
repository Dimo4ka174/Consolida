using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Abstract;

namespace DB.Entity
{
    [Table("MetrologicalInfo")]
    public class MetrologicalInfo : IEntity
    {
        [Key]
        public int? Id { get; set; }

        public int? OrderId { get; set; }

        public int? OrderProductId { get; set; }

        [MaxLength(50)]
        public string RegistrationNumber { get; set; } = string.Empty;

        public DateTime ExpiryDate { get; set; }

        public bool IsDeleted { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("OrderProductId")]
        public virtual OrderProduct? OrderProduct { get; set; }
    }
}