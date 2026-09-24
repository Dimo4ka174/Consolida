using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Entity.Enum;
using DB.Abstract;

namespace DB.Entity
{
    [Table("Customers")]
    public class Customer : IEntity
    {
        [Key]
        public int? Id { get; set; }
        public int? CompanyId { get; set; }


        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? PositionJob { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public PreferredMethod PreferredMethod { get; set; }
        public bool IsDeleted { get; set; }


        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }
    }
}
