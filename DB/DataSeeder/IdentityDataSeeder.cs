using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using DB.Authorization;

namespace DB.DataSeeder
{
    public static class IdentityDataSeeder
    {
        public static async Task Seed(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger logger)
        {
            await SeedRoles(roleManager, logger);
            await SeedUsers(userManager, logger);
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            var rolesConfig = new Dictionary<string, string[]>
            {
                ["Admin"] = Permissions.Roles.All
                    .Concat(Permissions.Users.All)
                    .Concat(Permissions.Cities.All)
                    .Concat(Permissions.Companies.All)
                    .Concat(Permissions.Customers.All)
                    .Concat(Permissions.CodesTNVD.All)
                    .Concat(Permissions.Manufacturers.All)
                    .Concat(Permissions.Products.All)
                    .Concat(Permissions.MeasureUnits.All)
                    .Concat(Permissions.TaxTypes.All)
                    .Concat(Permissions.Orders.All)
                    .ToArray(),

                ["Manager"] = Permissions.Cities.All
                    .Concat(Permissions.Companies.All)
                    .Concat(Permissions.Customers.All)
                    .Concat(Permissions.CodesTNVD.All)
                    .Concat(Permissions.Manufacturers.All)
                    .Concat(Permissions.Products.All)
                    .Concat(Permissions.MeasureUnits.All)
                    .Concat(Permissions.TaxTypes.All)
                    .Concat(Permissions.Orders.All)
                    .ToArray(),

                ["Director"] = new[]
                {
                    Permissions.Cities.Read,
                    Permissions.Companies.Read,
                    Permissions.Customers.Read,
                    Permissions.CodesTNVD.Read,
                    Permissions.Manufacturers.Read,
                    Permissions.Products.Read,
                    Permissions.MeasureUnits.Read,
                    Permissions.TaxTypes.Read,
                    Permissions.Orders.Read,
                },

                ["User"] = Array.Empty<string>()
            };

            var rolesCreated = 0;
            var permissionsAdded = 0;

            foreach (var roleConfig in rolesConfig)
            {
                var roleName = roleConfig.Key;
                var role = await roleManager.FindByNameAsync(roleName);

                if (role == null)
                {
                    role = new IdentityRole(roleName);
                    await roleManager.CreateAsync(role);
                    rolesCreated++;
                }

                var currentClaims = await roleManager.GetClaimsAsync(role);
                var currentPermissions = currentClaims
                    .Where(c => c.Type == "Permission")
                    .Select(c => c.Value)
                    .ToHashSet();

                var newPermissions = roleConfig.Value.ToHashSet();

                if (!currentPermissions.SetEquals(newPermissions))
                {
                    foreach (var claim in currentClaims.Where(c => c.Type == "Permission"))
                        await roleManager.RemoveClaimAsync(role, claim);

                    foreach (var permission in roleConfig.Value)
                    {
                        await roleManager.AddClaimAsync(role, new Claim("Permission", permission));
                        permissionsAdded++;
                    }
                }
            }

            if (rolesCreated > 0 || permissionsAdded > 0)
                logger.LogInformation("Roles: created {RolesCreated}, permissions synced {PermissionsAdded}", rolesCreated, permissionsAdded);
            else
                logger.LogDebug("Roles already exist, skipping creation");
        }

        private static async Task SeedUsers(UserManager<ApplicationUser> userManager, ILogger logger)
        {
            var users = new[]
            {
                new { Login = "admin",    Role = "Admin",    Password = "root" },
                new { Login = "manager",  Role = "Manager",  Password = "root" },
                new { Login = "director", Role = "Director", Password = "root" }
            };

            var usersCreated = 0;

            foreach (var userInfo in users)
            {
                if (await userManager.FindByNameAsync(userInfo.Login) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = userInfo.Login,
                        Email = userInfo.Login,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, userInfo.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userInfo.Role);
                        usersCreated++;
                        logger.LogDebug("Created user '{Login}' with role '{Role}'", userInfo.Login, userInfo.Role);
                    }
                    else
                    {
                        logger.LogWarning("Failed to create user '{Login}': {Errors}",
                            userInfo.Login, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            if (usersCreated > 0)
                logger.LogInformation("Created {UsersCreated} users", usersCreated);
            else
                logger.LogDebug("Users already exist, skipping creation");
        }
    }
}
