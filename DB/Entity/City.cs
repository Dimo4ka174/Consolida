using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DB.Abstract;

namespace DB.Entity
{
    [Table("Cities")]
    public class City : IEntity
    {
        [Key]
        public int? Id { get; set; }


        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }


        public List<Company> Companies { get; set; } = new List<Company>();
    }
}
