using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Abstract;

namespace DB.Entity
{
    [Table("TaxType")]
    public class TaxType : IEntity
    {
        [Key]
        public int? Id { get; set; }
        public int? MeasureUnitId { get; set; }

        public string Name { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey("MeasureUnitId")]
        public virtual MeasureUnit? MeasureUnit { get; set; }

        public List<OrderTaxProduct> OrdersTax { get; set; } = new List<OrderTaxProduct>();
    }
}
