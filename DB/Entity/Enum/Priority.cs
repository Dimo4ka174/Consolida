using System.ComponentModel.DataAnnotations;

namespace DB.Entity.Enum
{
    public enum Priority
    {
        [Display(Name = "Высокий")]
        High = 1,
        [Display(Name = "Средний")]
        Medium = 2,
        [Display(Name = "Низкий")]
        Low = 3,
    }
}
