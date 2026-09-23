using System.ComponentModel.DataAnnotations;

namespace DB.Entity.Enum
{
    public enum PreferredMethod
    {
        [Display(Name = "Самолет")]
        Air = 1,
        [Display(Name = "Корабль")]
        Sea = 2,
        [Display(Name = "Авто")]
        Auto = 3,
        [Display(Name = "Ж/Д")]
        Railway = 4,
    }
}
