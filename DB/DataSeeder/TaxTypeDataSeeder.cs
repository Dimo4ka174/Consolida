using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DB.Entity;

namespace DB.DataSeeder
{
    public static class TaxTypeDataSeeder
    {
        private static readonly (string Name, decimal Cost, string Unit)[] Taxes =
        {
            ("Таможенная пошлина 5%",          5m,     "%"),
            ("Таможенная пошлина 10%",         10m,    "%"),
            ("Таможенная пошлина 15%",         15m,    "%"),
            ("НДС 13%",                        13m,    "%"),
            ("Акциз",                          0m,     "%"),
            ("Таможенный сбор",                750m,   "шт"),
            ("Утилизационный сбор",            2000m,  "шт"),
            ("Сбор за оформление",             500m,   "упак"),
            ("Дополнительная пошлина",         1000m,  "упак"),
            ("Транспортный сбор",              300m,   "кг"),
        };

        public static async Task Seed(AppDbContext context, ILogger logger)
        {
            if (context.TaxTypes.Any())
            {
                logger.LogDebug("TaxTypes already exist, skipping creation");
                return;
            }

            var units = await context.MeasureUnits
                .Where(u => !u.IsDeleted)
                .ToDictionaryAsync(u => u.Name);

            var taxes = new List<TaxType>();

            foreach (var row in Taxes)
            {
                if (!units.TryGetValue(row.Unit, out var unit))
                {
                    logger.LogWarning("MeasureUnit '{Unit}' not found, skipping tax '{Tax}'",
                        row.Unit, row.Name);
                    continue;
                }

                taxes.Add(new TaxType
                {
                    Name = row.Name,
                    Cost = row.Cost,
                    MeasureUnitId = unit.Id,
                    IsDeleted = false
                });
            }

            await context.TaxTypes.AddRangeAsync(taxes);
            await context.SaveChangesAsync();

            logger.LogInformation("Created {Count} tax types", taxes.Count);
        }
    }
}
