using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using DB.Entity.Enum;

namespace Application.ViewModels.CustomerModel
{
    public class CustomerDto
    {
        public int? Id { get; set; }

        [Display(Name = "Компания")]
        public int? CompanyId { get; set; }

        [Required(ErrorMessage = "Поле 'Имя' обязательно для заполнения.")]
        [Display(Name = "Имя")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Поле 'Фамилия' обязательно для заполнения.")]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Отчество")]
        public string? MiddleName { get; set; }

        [Display(Name = "Должность")]
        public string? PositionJob { get; set; }

        [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
        [Display(Name = "Электронная почта")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Некорректный номер телефона.")]
        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [Display(Name = "Предпочтительный способ связи")]
        public PreferredMethod PreferredMethod { get; set; }

        [Display(Name = "Наименование компании")]
        public string? CompanyName { get; set; }

        public List<SelectListItem> CompaniesList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PreferredMethodsList { get; set; } = new List<SelectListItem>();
    }
}
