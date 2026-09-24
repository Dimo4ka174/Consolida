using DB.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DB.DataSeeder
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger logger)
        {
            await IdentityDataSeeder.Seed(userManager, roleManager, logger);
            await CitiesDataSeeder.SeedCities(context, logger);
            await CompanyCustomerDataSeeder.Seed(context, logger);
        }
    }
}
