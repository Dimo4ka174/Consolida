using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DB.Abstract;

namespace DB.Entity
{
    [Table("ConsolidationWeightLimits")]
    public class ConsolidationWeightLimit : IEntity
    {
        [Key]
        public int? Id { get; set; }

        // Значение в кг
        public decimal Value { get; set; }

        // Является ли значение предустановленным
        public bool IsSystem { get; set; }

        // Кто создал
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }
    }
}
