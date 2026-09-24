using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using DB.Abstract;

namespace DB.Entity
{
    [Table("Products")]
    [Index(nameof(Name), nameof(Model))]
    public class Product : IEntity, ICreatedAtEntity
    {
        [Key]
        public int? Id { get; set; }
        public int? ManufacturerId { get; set; }
        public int? CodeTNVDId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsDeleted { get; set; }


        [ForeignKey("CodeTNVDId")]
        public virtual CodeTNVD? CodeTNVD { get; set; }

        [ForeignKey("ManufacturerId")]
        public virtual Manufacturer? Manufacturer { get; set; }
    }
}
