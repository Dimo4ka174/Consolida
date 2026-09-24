using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using DB.Entity.Enum;

namespace Application.ViewModels.CompanyModel
{
    public class CompanyDto
    {
        public int? Id { get; set; }

        [Display(Name = "Город")]
        public int? CityId { get; set; }

        [Display(Name = "Страна")]
        public Country Country { get; set; }


        [Required(ErrorMessage = "Поле 'Наименование' обязательно для заполнения.")]
        [Display(Name = "Наименование компании")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Поле 'Адрес' обязательно для заполнения.")]
        [Display(Name = "Адрес")]
        public string Address { get; set; } = string.Empty;


        public List<SelectListItem> CitiesList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> CountriesList { get; set; } = new List<SelectListItem>();        
    }
}
