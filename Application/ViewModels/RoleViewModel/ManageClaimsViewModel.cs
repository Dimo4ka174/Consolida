namespace Application.ViewModels.RoleViewModel
{
    public class ManageClaimsViewModel
    {
        public string RoleName { get; set; } = string.Empty;
        public List<ClaimSelection> Claims { get; set; } = new();
    }

    public class ClaimSelection
    {
        public string Category { get; set; } = string.Empty;
        public string ClaimType { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
