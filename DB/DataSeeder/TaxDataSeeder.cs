using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DB.Entity;

namespace DB.DataSeeder
{
    public static class TaxDataSeeder
    {
        public static async Task SeedMeasureUnits(AppDbContext context, ILogger logger)
        {
            if (context.MeasureUnits.Any())
            {
                logger.LogDebug("Measure units already exist, skipping creation");
                return;
            }

            var measureUnits = new List<MeasureUnit>
            {
                new() { Name = "%" },
                new() { Name = "₽" },
                new() { Name = "¥" },
                new() { Name = "$" },
                new() { Name = "£" }
            };

            await context.MeasureUnits.AddRangeAsync(measureUnits);
            await context.SaveChangesAsync();
            logger.LogInformation("Created {Count} measure units", measureUnits.Count);
        }

        public static async Task Seed(AppDbContext context, ILogger logger)
        {
            await SeedMeasureUnits(context, logger);

            if (context.TaxTypes.Any())
            {
                logger.LogDebug("Tax types already exist, skipping creation");
                return;
            }

            var mu = await context.MeasureUnits.ToDictionaryAsync(x => x.Name);

            var taxTypes = new List<TaxType>
            {
                new() { Name = "Комиссия банка",              MeasureUnit = mu["%"], Cost = 2.5m  },
                new() { Name = "Маржа",                       MeasureUnit = mu["%"], Cost = 30m   },
                new() { Name = "Не предвиденные расходы",     MeasureUnit = mu["%"], Cost = 10m   },
                new() { Name = "Пошлина",                     MeasureUnit = mu["%"], Cost = 0m    },
                new() { Name = "Первичная поверка",           MeasureUnit = mu["₽"], Cost = 15000m },
                new() { Name = "Таможенный брокер",           MeasureUnit = mu["₽"], Cost = 25000m },
                new() { Name = "Декларация соответствия",     MeasureUnit = mu["₽"], Cost = 20000m },
                new() { Name = "Таможенные сборы",            MeasureUnit = mu["₽"], Cost = 30000m },
                new() { Name = "Терминальные расходы",        MeasureUnit = mu["₽"], Cost = 20000m },
                new() { Name = "Доставка",                    MeasureUnit = mu["₽"], Cost = 20000m }
            };

            await context.TaxTypes.AddRangeAsync(taxTypes);
            await context.SaveChangesAsync();
            logger.LogInformation("Created {Count} tax types", taxTypes.Count);
        }
    }
}
