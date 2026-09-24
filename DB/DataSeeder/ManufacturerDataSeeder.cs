using Microsoft.Extensions.Logging;
using DB.Entity;

namespace DB.DataSeeder
{
    public static class ManufacturerDataSeeder
    {
        private static readonly string[] Manufacturers =
        {
            "Huawei Technologies",
            "Xiaomi Corporation",
            "Lenovo Group",
            "Hisense Group",
            "Haier Group",
            "Midea Group",
            "TCL Technology",
            "Gree Electric",
            "ZTE Corporation",
            "Foxconn Industrial Internet",
            "BYD Electronic",
            "CATL (Contemporary Amperex)",
            "Sany Heavy Industry",
            "XCMG Construction Machinery",
            "Zoomlion Heavy Industry",
            "Sinopec Petroleum Machinery",
            "CNPC Equipment Manufacturing",
            "Dongfang Electric",
            "Shanghai Electric",
            "Harbin Electric",
        };

        public static async Task Seed(AppDbContext context, ILogger logger)
        {
            if (context.Manufacturers.Any())
            {
                logger.LogDebug("Manufacturers already exist, skipping creation");
                return;
            }

            var manufacturers = Manufacturers.Select(name => new Manufacturer
            {
                Name = name,
                IsDeleted = false
            }).ToList();

            await context.Manufacturers.AddRangeAsync(manufacturers);
            await context.SaveChangesAsync();

            logger.LogInformation("Created {Count} manufacturers", manufacturers.Count);
        }
    }
}
