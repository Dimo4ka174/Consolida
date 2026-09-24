using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Abstract;

namespace DB.Entity
{
    [Table("OrderTaxProduct")]
    public class OrderTaxProduct : IEntity
    {
        [Key]
        public int? Id { get; set; }
        public int? TaxTypeId { get; set; }
        public int? OrderId { get; set; }
        public int? OrderProductId { get; set; }


        public decimal Cost { get; set; }
        public bool IsCalculated { get; set; }
        public bool IsDeleted { get; set; }


        [ForeignKey("TaxTypeId")]
        public virtual TaxType? TaxType { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
        [ForeignKey("OrderProductId")]
        public virtual OrderProduct? OrderProduct { get; set; }
    }
}
