using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Abstract;

namespace DB.Entity
{
    [Table("MeasureUnits")]
    public class MeasureUnit : IEntity
    {
        [Key]
        public int? Id { get; set; }


        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }


        public List<TaxType> TaxTypes { get; set; } = new List<TaxType>();
    }
}
