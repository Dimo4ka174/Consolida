using System.ComponentModel.DataAnnotations;

namespace Application.ViewModels.RoleViewModel
{
    public class RoleViewModel
    {
        [Required(ErrorMessage = "Поле 'Наименоваине роли' обязательно")]
        [Display(Name = "Наименоваине роли")]
        public string RoleName { get; set; } = string.Empty;
        public Dictionary<string, List<ClaimViewModel>> GroupedClaims { get; set; } = new();
    }
}
