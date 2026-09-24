using System.ComponentModel.DataAnnotations;

namespace Application.ViewModels.CodeTNVDModel
{
    public class CodeTNVDdto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Код ТН ВЭД обязателен для заполнения.")]
        [Display(Name = "Код ТН ВЭД")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ставка налога обязательна для заполнения.")]
        [Display(Name = "Ставка (%)")]
        public decimal Rate { get; set; }
    }
}
