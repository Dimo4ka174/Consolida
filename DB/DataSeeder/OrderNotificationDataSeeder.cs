using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DB.Entity;

namespace DB.DataSeeder
{
    public static class OrderNotificationDataSeeder
    {
        public static async Task Seed(AppDbContext context, ILogger logger)
        {
            if (context.OrderNotifications.Any(n => !n.IsDeleted))
            {
                logger.LogDebug("OrderNotifications already exist, skipping creation");
                return;
            }

            var orders = await context.Orders
                .Where(o => !o.IsDeleted)
                .OrderBy(o => o.Id)
                .Take(3)
                .Select(o => new { o.Id, o.OrderNumber })
                .ToListAsync();

            if (orders.Count < 3)
            {
                logger.LogWarning("Not enough orders to seed notifications (found {Count})", orders.Count);
                return;
            }

            var today = DateTime.Today;

            var notifications = new List<OrderNotification>
            {
                new()
                {
                    OrderId = orders[0].Id,
                    Title = "Уточнить сроки у поставщика",
                    Description = $"Связаться с производителем по заказу №{orders[0].OrderNumber}, уточнить финальную дату отгрузки.",
                    DueDate = today.AddDays(3),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "admin",
                    IsCompleted = false,
                    IsDeleted = false
                },
                new()
                {
                    OrderId = orders[1].Id,
                    Title = "Проверить поступление оплаты",
                    Description = $"Проверить приход денежных средств по заказу №{orders[1].OrderNumber}.",
                    DueDate = today.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "admin",
                    IsCompleted = false,
                    IsDeleted = false
                },
                new()
                {
                    OrderId = orders[2].Id,
                    Title = "Подготовить документы для таможни",
                    Description = $"Собрать пакет документов для таможенного оформления заказа №{orders[2].OrderNumber}.",
                    DueDate = today.AddDays(30),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "admin",
                    IsCompleted = false,
                    IsDeleted = false
                }
            };

            await context.OrderNotifications.AddRangeAsync(notifications);
            await context.SaveChangesAsync();

            logger.LogInformation("Created {Count} order notifications", notifications.Count);
        }
    }
}
