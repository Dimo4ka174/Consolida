using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Abstract;

namespace DB.Entity
{
    [Table("Manufacturers")]
    public class Manufacturer : IEntity
    {
        [Key]
        public int? Id { get; set; }


        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }


        public List<Product> Products { get; set; } = new List<Product>();
    }
}
