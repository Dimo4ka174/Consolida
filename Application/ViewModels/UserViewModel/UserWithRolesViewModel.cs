namespace Application.ViewModels.UserViewModel
{
    public class UserWithRolesViewModel
    {
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public IList<string>? Roles { get; set; }
    }
}
