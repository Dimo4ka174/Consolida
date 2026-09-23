using System.ComponentModel.DataAnnotations;

namespace Application.ViewModels.UserViewModel
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Логин обязателен")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty.ToString();

        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}
