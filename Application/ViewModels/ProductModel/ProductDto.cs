using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Application.ViewModels.ProductModel
{
    public class ProductDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Поле 'Производитель' обязательно для заполнения.")]
        [Display(Name = "Производитель")]
        public int? ManufacturerId { get; set; }


        [Required(ErrorMessage = "Поле 'Модель' обязательно для заполнения.")]
        [Display(Name = "Модель")]
        public string Model { get; set; } = string.Empty;

        [Required(ErrorMessage = "Поле 'Наименование товар' обязательно для заполнения.")]
        [Display(Name = "Наименование Товара")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Поле 'Стоимость' обязательно для заполнения.")]
        [Display(Name = "Стоимость (¥)")]
        public decimal Cost { get; set; }

        public DateTime CreatedDate { get; set; }

        public List<SelectListItem> ManufacturersList { get; set; } = new List<SelectListItem>();        
    }
}
