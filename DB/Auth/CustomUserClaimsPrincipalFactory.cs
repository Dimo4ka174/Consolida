using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using DB.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DB.Auth
{
    public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        private readonly ILogger<CustomUserClaimsPrincipalFactory> _logger;

        public CustomUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<CustomUserClaimsPrincipalFactory> logger)
            : base(userManager, roleManager, optionsAccessor)
        {
            _logger = logger;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            var roles = await UserManager.GetRolesAsync(user);
            _logger.LogInformation($"Generating stamps for the user {user.UserName}. Role: {string.Join(", ", roles)}.");

            var displayName = BuildDisplayName(user);
            identity.AddClaim(new Claim("DisplayName", displayName));

            foreach (var role in roles.Distinct())
            {
                if (!identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }

                var roleEntity = await RoleManager.FindByNameAsync(role);
                if (roleEntity != null)
                {
                    var roleClaims = await RoleManager.GetClaimsAsync(roleEntity);
                    foreach (var claim in roleClaims)
                    {
                        if (!identity.HasClaim(c => c.Type == claim.Type && c.Value == claim.Value))
                        {
                            identity.AddClaim(claim);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"Role {role} not found in RoleManager.");
                }
            }

            return identity;
        }

        private static string BuildDisplayName(ApplicationUser user)
        {
            // Собираем "Фамилия Имя" из доступных частей
            var parts = new[] { user.LastName, user.FirstName }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            if (parts.Length > 0)
                return string.Join(" ", parts);

            // Fallback — логин/email
            return !string.IsNullOrWhiteSpace(user.UserName)
                ? user.UserName
                : (user.Email ?? "Unknown");
        }
    }
}
