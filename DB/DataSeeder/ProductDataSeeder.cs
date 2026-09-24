using DB.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DB.DataSeeder
{
    public static class ProductDataSeeder
    {
        // (Название, Модель, Производитель, КодТНВЭД, Цена ¥)
        private static readonly (string Name, string Model, string Manufacturer, string Code, decimal Price)[] Products =
        {
            // Huawei
            ("Ноутбук",              "MateBook D14",         "Huawei Technologies",       "8471",  4500m),
            ("Смартфон",             "Nova 12 Pro",          "Huawei Technologies",       "8517",  3200m),
            ("Планшет",              "MatePad 11",           "Huawei Technologies",       "8471",  2800m),
            ("Маршрутизатор",        "AX3 Pro",              "Huawei Technologies",       "8517",   680m),
            ("Коммутатор",           "S5700-28X",            "Huawei Technologies",       "8517",  5400m),

            // Xiaomi
            ("Смартфон",             "Redmi Note 13 Pro",    "Xiaomi Corporation",        "8517",  2100m),
            ("Смартфон",             "Xiaomi 14",            "Xiaomi Corporation",        "8517",  4200m),
            ("Планшет",              "Pad 6",                "Xiaomi Corporation",        "8471",  1900m),
            ("Робот-пылесос",        "Roborock S8",          "Xiaomi Corporation",        "8508",  3400m),
            ("Фитнес-браслет",       "Smart Band 8",         "Xiaomi Corporation",        "8517",   280m),

            // Lenovo
            ("Ноутбук",              "ThinkPad X1 Carbon",   "Lenovo Group",              "8471", 12800m),
            ("Ноутбук",              "Legion 5 Pro",         "Lenovo Group",              "8471",  9800m),
            ("Монитор",              "ThinkVision T27h",     "Lenovo Group",              "8528",  2400m),
            ("Рабочая станция",      "ThinkStation P360",    "Lenovo Group",              "8471", 18500m),

            // Hisense
            ("Телевизор",            "65E7K",                "Hisense Group",             "8528",  4200m),
            ("Телевизор",            "55U8K",                "Hisense Group",             "8528",  5800m),
            ("Холодильник",          "RB-600N4",             "Hisense Group",             "8418",  3600m),

            // Haier
            ("Холодильник",          "CEF535AWG",            "Haier Group",               "8418",  4100m),
            ("Стиральная машина",    "HW100-B14979",         "Haier Group",               "8450",  3800m),
            ("Кондиционер",          "AS20S2SF",             "Haier Group",               "8415",  3200m),

            // Midea
            ("Кондиционер",          "MSAG-09HRN8",          "Midea Group",               "8415",  2800m),
            ("Микроволновка",        "MEC-20ESS",            "Midea Group",               "8516",   680m),
            ("Пылесос",              "V12 Pro",              "Midea Group",               "8508",  1900m),

            // TCL
            ("Телевизор",            "55C655",               "TCL Technology",            "8528",  2900m),
            ("Смартфон",             "50 SE",                "TCL Technology",            "8517",   980m),

            // Gree
            ("Кондиционер",          "GWH12AGB",             "Gree Electric",             "8415",  3100m),

            // ZTE
            ("Маршрутизатор",        "ZXHN H288A",           "ZTE Corporation",           "8517",   420m),
            ("Коммутатор",           "ZXR10 5950",           "ZTE Corporation",           "8517",  8900m),

            // Foxconn
            ("Сервер",               "FII HPC-1000",         "Foxconn Industrial Internet", "8471", 24000m),

            // Sany
            ("Экскаватор",           "SY215C",               "Sany Heavy Industry",       "8429", 280000m),
            ("Погрузчик",            "SWL3220",              "Sany Heavy Industry",       "8429", 160000m),

            // XCMG
            ("Автокран",             "XCT25L4",              "XCMG Construction Machinery", "8705", 420000m),

            // BYD
            ("Аккумулятор",          "Blade Battery 60kWh",  "BYD Electronic",            "8507",  85000m),

            // CATL
            ("Аккумулятор",          "LFP 280Ah",            "CATL (Contemporary Amperex)", "8507",  9800m),
        };

        public static async Task Seed(AppDbContext context, ILogger logger)
        {
            if (context.Products.Any())
            {
                logger.LogDebug("Products already exist, skipping creation");
                return;
            }

            var manufacturers = await context.Manufacturers
                .Where(m => !m.IsDeleted)
                .ToDictionaryAsync(m => m.Name);

            var codes = await context.CodesTNVD
                .Where(c => !c.IsDeleted)
                .ToDictionaryAsync(c => c.Name);

            var products = new List<Product>();

            foreach (var row in Products)
            {
                if (!manufacturers.TryGetValue(row.Manufacturer, out var manufacturer))
                {
                    logger.LogWarning("Manufacturer '{Name}' not found, skipping product '{Product}'",
                        row.Manufacturer, row.Name);
                    continue;
                }

                if (!codes.TryGetValue(row.Code, out var code))
                {
                    logger.LogWarning("CodeTNVD '{Code}' not found, skipping product '{Product}'",
                        row.Code, row.Name);
                    continue;
                }

                products.Add(new Product
                {
                    Name = row.Name,
                    Model = row.Model,
                    Price = row.Price,
                    ManufacturerId = manufacturer.Id,
                    CodeTNVDId = code.Id,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow
                });
            }

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            logger.LogInformation("Created {Count} products", products.Count);
        }
    }
}
