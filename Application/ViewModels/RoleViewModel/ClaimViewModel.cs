using System.ComponentModel.DataAnnotations;

namespace Application.ViewModels.RoleViewModel
{
    public class ClaimViewModel
    {
        [Required(ErrorMessage = "Имя роли обязательно")]
        public string RoleName { get; set; } = string.Empty;
        public string ClaimType { get; set; } = "Permission";
        [Required(ErrorMessage = "Значение клайма обязательно")]
        public string ClaimValue { get; set; } = string.Empty;
    }
}