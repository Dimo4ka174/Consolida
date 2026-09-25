using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DB.Entity;

namespace Application.DataAccessLayer.Jobs
{
    public class DeliveryNotificationJob
    {
        private readonly IConsolidationService _consolidationService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeliveryNotificationJob> _logger;

        public DeliveryNotificationJob(
            IConsolidationService consolidationService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<DeliveryNotificationJob> logger)
        {
            _consolidationService = consolidationService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var toEmail = _configuration["EmailSettings:ToEmail"];
            if (string.IsNullOrEmpty(toEmail))
            {
                _logger.LogWarning("EmailSettings:ToEmail не задан, уведомления отключены");
                return;
            }

            var upcomingPools = await _consolidationService.GetPoolsForDeliveryNotificationAsync(3);
            foreach (var pool in upcomingPools)
            {
                var subject = $"Напоминание: доставка отгрузки №{pool.Id} ожидается {pool.ExpectedDeliveryDate:dd.MM.yyyy}";
                var body = BuildEmailBody(pool);
                await _emailService.SendEmailAsync(toEmail, subject, body);
            }
        }

        private string BuildEmailBody(ConsolidationPool pool)
        {
            // Формируем HTML
            var orders = pool.Orders.Select(o => $"<li>Заказ №{o.OrderNumber} — {o.Customer?.Company?.Name}, вес {o.TotalWeight} кг</li>");
            return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2 style='color: #f0ad4e;'>Приближается дата доставки</h2>
                <p>Отгрузка №{pool.Id} ожидается {pool.ExpectedDeliveryDate:dd.MM.yyyy}.</p>
                <ul>{string.Join("", orders)}</ul>
                <p>Пожалуйста, будьте готовы к приёмке.</p>
                <hr/>
                <small>Проверка выполнена: {DateTime.Now:dd.MM.yyyy HH:mm}</small>
            </body>
            </html>";
        }
    }
}