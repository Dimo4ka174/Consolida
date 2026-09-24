using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DB.Entity.Enum;
using DB.Entity;

namespace DB.DataSeeder
{
    public static class CompanyCustomerDataSeeder
    {
        private static readonly (string CompanyName, string CityName, string Address, string[] Customers)[] Companies =
        {
            ("ООО «ТехноСервис»",     "Москва",            "ул. Ленина, 10",   new[] { "Иванов Иван Иванович", "Петров Петр Петрович" }),
            ("АО «ПромАвтоматика»",   "Санкт-Петербург",   "пр. Невский, 25",  new[] { "Сидоров Алексей Владимирович", "Кузнецов Дмитрий Андреевич" }),
            ("ООО «НефтеГазСервис»",  "Екатеринбург",      "ул. Мира, 7",      new[] { "Морозов Сергей Николаевич" }),
            ("ООО «ЭнергоСтрой»",     "Новосибирск",       "ул. Кирова, 15",   new[] { "Волков Андрей Петрович", "Соколов Виктор Иванович" }),
            ("ООО «ГазТехнолоджи»",   "Казань",            "ул. Баумана, 3",   new[] { "Попов Николай Сергеевич" })
        };

        public static async Task Seed(AppDbContext context, ILogger logger)
        {
            if (context.Companies.Any() || context.Customers.Any())
            {
                logger.LogDebug("Companies/Customers already exist, skipping creation");
                return;
            }

            var cities = await context.Cities.ToDictionaryAsync(c => c.Name);

            foreach (var row in Companies)
            {
                if (!cities.TryGetValue(row.CityName, out var city))
                {
                    logger.LogWarning("City '{City}' not found, skipping company '{Company}'",
                        row.CityName, row.CompanyName);
                    continue;
                }

                var company = new Company
                {
                    Name = row.CompanyName,
                    CityId = city.Id,
                    Country = Country.Russia,
                    Address = row.Address,
                    IsDeleted = false
                };
                await context.Companies.AddAsync(company);
                await context.SaveChangesAsync();

                // Служебные клиенты, как в OrderAPIController
                await context.Customers.AddAsync(new Customer
                {
                    CompanyId = company.Id,
                    FirstName = "",
                    LastName = "Заинтересованные лица",
                    IsDeleted = false
                });
                await context.Customers.AddAsync(new Customer
                {
                    CompanyId = company.Id,
                    FirstName = "",
                    LastName = "–",
                    IsDeleted = false
                });

                foreach (var fullName in row.Customers)
                {
                    var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var lastName = parts.Length > 0 ? parts[0] : "";
                    var firstName = parts.Length > 1 ? parts[1] : "";
                    var middleName = parts.Length > 2 ? parts[2] : null;

                    await context.Customers.AddAsync(new Customer
                    {
                        CompanyId = company.Id,
                        LastName = lastName,
                        FirstName = firstName,
                        MiddleName = middleName,
                        IsDeleted = false
                    });
                }
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Created {Count} companies with customers", Companies.Length);
        }
    }
}
