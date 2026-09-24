using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DB.Entity.Enum;
using DB.Entity;

namespace DB.DataSeeder
{
    public static class OrderDataSeeder
    {
        private static readonly (string Name, string Model, string Manufacturer, decimal Price, int Quantity, int LeadTime)[] Products =
        {
            ("Electronic, Prowirl 200 Index C", "XPD0043-A2AA00", "Endress+Hauser", 5500m, 1, 9),
            ("I/O module, 2-wire, 4-20mA, HART, Ex", "XPD0015-AA00", "Endress+Hauser", 3500m, 1, 9),
            ("Display module, SD02, push buttons", "XPD0024-AC00", "Endress+Hauser", 3900m, 1, 9),
            ("Kit pre-amplifier, Prowirl 200", "71222996", "Endress+Hauser", 1950m, 1, 9),
            ("Flowmeter Promag P 300", "5P3B6H-BDDCACGFABED3SGAA2+AAEBZ1", "Endress+Hauser", 153000m, 1, 9),
            ("O-ring", "K9772TH", "Yokogawa", 10000m, 1, 9),
            ("Sealing ring", "K9772TJ", "Yokogawa", 13600m, 1, 9),
            ("Tunable Diode Laser Spectrometer", "TDLS8000-N1-X1-D5-A1-N-N/RX/SCT", "Yokogawa", 5500m, 1, 9),
            ("Isolation Flange for TDLS8000", "IF8000-50-50-SS-12-N", "Yokogawa", 3500m, 1, 7),
            ("Calibration Cell with free-standing frame for O2", "K9772XA", "Yokogawa", 1950m, 1, 6),
            ("Cable 10m", "K9775XB", "Yokogawa", 100m, 1, 9),
            ("O-ring", "K9771JN", "Yokogawa", 1000m, 1, 9),
            ("Pressure sensor", "EJA130E-DHS5G-717NN/KU22", "Yokogawa", 5000m, 2, 11),
            ("Pressure sensor", "EJA440E-JCS4G-719ED/KU22", "Yokogawa", 5300m, 5, 11),
            ("Pressure sensor", "EJX110A-JFS5G-919EN/KU22/N4", "Yokogawa", 5600m, 4, 11),
            ("Pressure sensor", "EJA110E-JHS5G-717ND/KU22", "Yokogawa", 3900m, 5, 11),
            ("Pressure sensor", "EJA430E-DBT4G-917NN/KU22", "Yokogawa", 4500m, 4, 11),
            ("Pressure sensor", "EJA530E-JCS9N-014NN", "Yokogawa", 2800m, 3, 5),
            ("Pressure sensor", "EJA530E-JDS7N-019EL/KU22", "Yokogawa", 3400m, 3, 11),
            ("Micro Motion H100S Hygienic Coriolis Meter", "H100S138N6EZEZZZZ", "Emerson", 95000m, 1, 12),
            ("Преобразователь давления 3051", "3051CD2A22A1AB4(HR7/HR5)", "Emerson", 4300m, 1, 12),
            ("Преобразователь давления 3051", "3051CD2A22A1AB4Q4I1(HR7/HR5)", "Emerson", 4400m, 1, 13),
            ("Преобразователь давления 3051", "3051TG3A2B21AB4(HR7/HR5)", "Emerson", 3400m, 1, 14),
            ("Преобразователь давления 3051", "3051TG1A2B21AB4(HR7/HR5)", "Emerson", 3400m, 1, 15),
            ("Преобразователь давления 3051", "3051TG4A2B21AB4(HR7/HR5)", "Emerson", 3500m, 1, 16),
            ("waveguide level gauge", "5301HS2S1V4AM00075CBEMHR7P1C1Q4Q8QGQTT1R7627", "Rosemount", 3000m, 1, 8),
            ("waveguide level gauge", "5301HA2S1V4AM00095CAEMHR7P1C1Q4Q8QGQ76T1R7813", "Rosemount", 3500m, 1, 8),
            ("radar level gauge", "5301HA1S1E4AM00070CBEMHR7Q4WR5R3013", "Rosemount", 4000m, 1, 8),
        };

        // Имена существующих производителей из ManufacturerDataSeeder
        private static readonly string[] ExtraManufacturers = { "Endress+Hauser", "Yokogawa", "Emerson", "Rosemount" };

        public static async Task Seed(AppDbContext context, ILogger logger)
        {
            if (context.Orders.Any(o => !o.IsDeleted))
            {
                logger.LogDebug("Orders already exist, skipping creation");
                return;
            }

            // 1. Создаём недостающих производителей
            await EnsureManufacturersAsync(context, logger);
            await context.SaveChangesAsync();

            // 2. Создаём продукты из Excel (если их нет)
            await EnsureProductsAsync(context, logger);
            await context.SaveChangesAsync();

            // 3. Загружаем зависимости
            var companies = await context.Companies
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            var customers = await context.Customers
                .Where(c => !c.IsDeleted && c.LastName != "Заинтересованные лица" && c.LastName != "–")
                .ToListAsync();

            var allTaxTypes = await context.TaxTypes
                .Where(t => !t.IsDeleted)
                .ToListAsync();

            var allProducts = await context.Products
                .Where(p => !p.IsDeleted && ExtraManufacturers.Contains(p.Manufacturer!.Name))
                .Include(p => p.Manufacturer)
                .ToListAsync();

            if (!companies.Any() || !customers.Any() || !allProducts.Any())
            {
                logger.LogWarning("Недостаточно данных для создания заказов (companies={Companies}, customers={Customers}, products={Products})",
                    companies.Count, customers.Count, allProducts.Count);
                return;
            }

            // 4. Создаём 10 заказов
            var rnd = new Random(42); // фиксированный seed для воспроизводимости
            var currentYear = DateTime.Now.ToString("yy");
            var baseDate = DateTime.Now.AddDays(-60);

            for (int i = 1; i <= 10; i++)
            {
                var customer = customers[(i - 1) % customers.Count];
                var orderDate = baseDate.AddDays(i * 5);
                var orderNumber = $"{1000 + i}/{currentYear}";

                // Берём 2-5 случайных товаров
                var productCount = rnd.Next(2, 6);
                var selectedProducts = allProducts
                    .OrderBy(_ => rnd.Next())
                    .Take(productCount)
                    .ToList();

                // Считаем итоги
                decimal totalWeight = 0m;
                decimal totalCost = 0m;

                foreach (var p in selectedProducts)
                {
                    var quantity = rnd.Next(1, 4);
                    totalWeight += quantity * 1.5m; // условный вес 1.5 кг на единицу
                    totalCost += quantity * p.Price;
                }

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    CustomerId = customer.Id,
                    CreationDate = orderDate,
                    LastChangeDate = orderDate.AddDays(rnd.Next(1, 20)),
                    DateCreationTKP = orderDate.AddDays(2),
                    LastStatusChangeDate = orderDate.AddDays(rnd.Next(5, 25)),
                    Priority = Priority.Medium,
                    Status = Status.Paid,
                    ExchangeRate = 12.5m + (decimal)rnd.NextDouble() * 0.5m,
                    TotalWeight = totalWeight,
                    TotalCost = totalCost,
                    Comment = i % 3 == 0 ? "Срочная поставка" : null,
                    IsDeleted = false
                };

                await context.Orders.AddAsync(order);
                await context.SaveChangesAsync();

                // 5. OrderProduct + OrderTaxProduct
                foreach (var p in selectedProducts)
                {
                    var quantity = rnd.Next(1, 4);
                    var orderProduct = new OrderProduct
                    {
                        OrderId = order.Id,
                        ProductId = p.Id,
                        Quantity = quantity,
                        Price = p.Price,
                        Weight = 1.5m,
                        TotalPrice = quantity * p.Price,
                        DeliveryDate = orderDate.AddDays(p.Manufacturer!.Name == "Endress+Hauser" ? 63 : 56),
                        LeadTime = 9,
                        Comment = string.Empty,
                        IsDeleted = false
                    };
                    await context.OrdersProducts.AddAsync(orderProduct);
                    await context.SaveChangesAsync();

                    // Для каждого OrderProduct добавляем OrderTaxProduct по каждому налогу
                    foreach (var tax in allTaxTypes)
                    {
                        await context.OrdersTaxProducts.AddAsync(new OrderTaxProduct
                        {
                            OrderId = order.Id,
                            OrderProductId = orderProduct.Id,
                            TaxTypeId = tax.Id,
                            Cost = tax.Cost,
                            IsCalculated = false,
                            IsDeleted = false
                        });
                    }
                }

                // 6. OrderTax — по каждому налогу
                foreach (var tax in allTaxTypes)
                {
                    await context.OrdersTaxes.AddAsync(new OrderTax
                    {
                        OrderId = order.Id,
                        TaxTypeId = tax.Id,
                        Cost = tax.Cost,
                        IsCalculated = false,
                        IsDeleted = false
                    });
                }

                // 7. История статусов — путь от None до Paid
                var statusPath = new[]
                {
                    Status.Registered,
                    Status.Calculated,
                    Status.ExhibitTKP,
                    Status.TransferredProduction,
                    Status.RequiresPayment,
                    Status.Paid
                };

                var statusDate = orderDate;
                var previous = Status.None;

                foreach (var status in statusPath)
                {
                    statusDate = statusDate.AddDays(rnd.Next(2, 8));
                    await context.OrderStatusHistory.AddAsync(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        OldStatus = previous,
                        NewStatus = status,
                        ChangeDate = statusDate,
                        ChangedBy = "admin",
                        IsDeleted = false
                    });
                    previous = status;
                }

                await context.SaveChangesAsync();
            }

            logger.LogInformation("Created 10 orders with status Paid");
        }

        private static async Task EnsureManufacturersAsync(AppDbContext context, ILogger logger)
        {
            var existing = await context.Manufacturers
                .Select(m => m.Name)
                .ToListAsync();

            var toAdd = ExtraManufacturers
                .Where(name => !existing.Contains(name))
                .Select(name => new Manufacturer { Name = name, IsDeleted = false })
                .ToList();

            if (toAdd.Any())
            {
                await context.Manufacturers.AddRangeAsync(toAdd);
                logger.LogInformation("Created {Count} extra manufacturers for orders", toAdd.Count);
            }
        }

        private static async Task EnsureProductsAsync(AppDbContext context, ILogger logger)
        {
            var manufacturers = await context.Manufacturers
                .Where(m => ExtraManufacturers.Contains(m.Name))
                .ToDictionaryAsync(m => m.Name, m => m.Id);

            // Находим какой-нибудь CodeTNVD по умолчанию (для приборов — 9027)
            var defaultCode = await context.CodesTNVD
                .FirstOrDefaultAsync(c => c.Name == "9027");

            var existingModels = await context.Products
                .Select(p => p.Model)
                .ToListAsync();

            var toAdd = new List<Product>();

            foreach (var row in Products)
            {
                if (existingModels.Contains(row.Model))
                    continue;

                if (!manufacturers.TryGetValue(row.Manufacturer, out var manufacturerId))
                    continue;

                toAdd.Add(new Product
                {
                    Name = row.Name,
                    Model = row.Model,
                    ManufacturerId = manufacturerId,
                    CodeTNVDId = defaultCode?.Id,
                    Price = row.Price,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = false
                });
            }

            if (toAdd.Any())
            {
                await context.Products.AddRangeAsync(toAdd);
                logger.LogInformation("Created {Count} products from Excel for orders", toAdd.Count);
            }
        }
    }
}
