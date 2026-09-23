using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text;
using System;

namespace DB.Abstract
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
