using System.ComponentModel.DataAnnotations;
using DB.Entity;

namespace Application.ViewModels.CityModel
{
    public class CityDto
    {
        public int? Id { get; set; }


        [Required(ErrorMessage = "Поле 'Наименование' обязательно для заполнения.")]
        [Display(Name = "Наименование города")]
        public string Name { get; set; } = string.Empty;
        public void Initialize(City city)
        {
            Id = city.Id;
            Name = city.Name;
        }
    }
}
