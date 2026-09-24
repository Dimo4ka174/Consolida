using System.ComponentModel.DataAnnotations;

namespace Application.ViewModels.MeasureUnitModel
{
    public class MeasureUnitDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Поле 'Наименование единицы изменения' обязательно для заполнения.")]
        [Display(Name = "Наименование единицы изменения")]
        public string Name { get; set; } = string.Empty;   
    }
}
