using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace DB.Authorization
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        [Required(ErrorMessage = "Имя обязательно для заполнения.")]
        [Column(TypeName = "varchar(100)")]
        [Display(Name = "Имя")]
        public string FirstName { get; set; } = string.Empty;

        [PersonalData]
        [Required(ErrorMessage = "Фамилия обязательна для заполнения.")]
        [Column(TypeName = "varchar(100)")]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [PersonalData]
        [Column(TypeName = "varchar(100)")]
        [Display(Name = "Отчество")]
        public string? MiddleName { get; set; }
    }
}
