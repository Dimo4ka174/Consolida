using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Entity.Enum;
using DB.Abstract;

namespace DB.Entity
{
    [Table("Companies")]
    public class Company : IEntity
    {
        [Key]
        public int? Id { get; set; }
        public int? CityId { get; set; }


        public string Name { get; set; } = string.Empty;
        public Country Country { get; set; }
        public string Address { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }


        public virtual City? City { get; set; }
        public List<Customer> Customers { get; set; } = new List<Customer>();
    }
}
