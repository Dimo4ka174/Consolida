using DB.Entity;
using Microsoft.Extensions.Logging;

namespace DB.DataSeeder
{
    public static class CitiesDataSeeder
    {
        public static async Task SeedCities(AppDbContext context, ILogger logger)
        {
            if (!context.Cities.Any())
            {
                var cities = new List<City>
                {
                    new() { Name = "Москва" },
                    new() { Name = "Санкт-Петербург" },
                    new() { Name = "Новосибирск" },
                    new() { Name = "Екатеринбург" },
                    new() { Name = "Казань" },
                    new() { Name = "Красноярск" },
                    new() { Name = "Нижний Новгород" },
                    new() { Name = "Челябинск" },
                    new() { Name = "Уфа" },
                    new() { Name = "Самара" },
                    new() { Name = "Ростов-на-Дону" },
                    new() { Name = "Краснодар" },
                    new() { Name = "Омск" },
                    new() { Name = "Воронеж" },
                    new() { Name = "Пермь" },
                    new() { Name = "Волгоград" },
                    new() { Name = "Саратов" },
                    new() { Name = "Тюмень" },
                    new() { Name = "Тольятти" },
                    new() { Name = "Махачкала" },
                    new() { Name = "Шанхай" },
                    new() { Name = "Пекин" },
                    new() { Name = "Тяньцзинь" },
                    new() { Name = "Шэньчжэнь" },
                    new() { Name = "Гуанчжоу" },
                    new() { Name = "Чэнду" },
                    new() { Name = "Чунцин" },
                    new() { Name = "Дунгуань" },
                    new() { Name = "Шэньян" },
                    new() { Name = "Ухань" },
                    new() { Name = "Сеул" },
                };

                await context.Cities.AddRangeAsync(cities);
                await context.SaveChangesAsync();

                logger.LogInformation("Created {CityCount} cities", cities.Count);
            }
            else
            {
                logger.LogDebug("Cities already exist, skipping creation");
            }
        }
    }
}
