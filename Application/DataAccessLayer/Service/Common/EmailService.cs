using Application.DataAccessLayer.Interface.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Application.DataAccessLayer.Service.Common
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IHostEnvironment _environment;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }


        public async Task SendEmailAsync(string to, string subject, string body)
        {
            if (_environment.IsDevelopment())
            {
                _logger.LogInformation("[DEV MODE] Email would be sent to {To} with subject {Subject}.", to, subject);
                return;
            }

            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var username = _configuration["EmailSettings:SmtpUsername"];
                var password = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];

                if (string.IsNullOrEmpty(password))
                    throw new InvalidOperationException("SMTP password is not configured. Set EmailSettings__SmtpPassword environment variable.");

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fromEmail))
                    throw new InvalidOperationException("SMTP configuration incomplete. Check SmtpServer, SmtpUsername, FromEmail.");

                // 1. Создаем сообщение с помощью MimeKit
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("", fromEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                // 2. Указываем, что тело письма - это HTML
                var bodyBuilder = new BodyBuilder { HtmlBody = body };
                message.Body = bodyBuilder.ToMessageBody();

                // 3. Используем SmtpClient из MailKit
                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.Auto); // SSL/TLS
                await client.AuthenticateAsync(username, password);// Аутентификация
                await client.SendAsync(message);// Отправка
                await client.DisconnectAsync(true);// Корректное отключение

                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }
    }
}