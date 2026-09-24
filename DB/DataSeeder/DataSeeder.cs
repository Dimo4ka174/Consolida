using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DB.Authorization;

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
            await CodeTNVDDataSeeder.Seed(context, logger);
            await ManufacturerDataSeeder.Seed(context, logger);
            await ProductDataSeeder.Seed(context, logger);
        }
    }
}
