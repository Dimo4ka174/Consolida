using System.ComponentModel.DataAnnotations;


namespace Application.ViewModels.ManufacturerModel
{
    public class ManufacturerDto
    {
        public int? Id { get; set; }
        
        [Required(ErrorMessage = "Поле 'Наименование производителя' обязательно для заполнения")]
        [Display(Name = "Наименование производителя")]
        public string Name { get; set; } = string.Empty;
    }
}
