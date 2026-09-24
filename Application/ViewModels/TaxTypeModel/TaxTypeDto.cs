using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Application.ViewModels.TaxTypeModel
{
    public class TaxTypeDto
    {
        public int? Id { get; set; }

        [Display(Name = "Единицы измерения")]
        public int? MeasureUnitId { get; set; }

        [Required(ErrorMessage = "Поле 'Наименование налога' обязательно для заполнения.")]
        [Display(Name = "Наименование налога")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Поле 'Стоимость налога' обязательно для заполнения.")]
        [Display(Name = "Стоимость налога")]
        public decimal Cost { get; set; }

        public List<SelectListItem> MeasureUnitsList { get; set; } = new List<SelectListItem>();
    }
}
