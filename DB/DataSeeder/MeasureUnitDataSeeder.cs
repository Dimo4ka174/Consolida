using Microsoft.Extensions.Logging;
using DB.Entity;

namespace DB.DataSeeder
{
    public static class MeasureUnitDataSeeder
    {
        private static readonly string[] Units =
        {
            "шт",
            "компл",
            "упак",
            "кг",
            "т",
            "м",
            "м²",
            "м³",
            "л",
            "%",
        };

        public static async Task Seed(AppDbContext context, ILogger logger)
        {
            if (context.MeasureUnits.Any())
            {
                logger.LogDebug("MeasureUnits already exist, skipping creation");
                return;
            }

            var units = Units.Select(u => new MeasureUnit { Name = u, IsDeleted = false }).ToList();
            await context.MeasureUnits.AddRangeAsync(units);
            await context.SaveChangesAsync();

            logger.LogInformation("Created {Count} measure units", units.Count);
        }
    }
}
