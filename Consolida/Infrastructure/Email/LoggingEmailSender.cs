namespace Consolida.Infrastructure.Email
{
    /// <summary>
    /// Заглушка-отправитель: пишет письмо в лог.
    /// Для прода заменим на SMTP-реализацию.
    /// </summary>
    public class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation(
                "Email (stub) → To: {Email} | Subject: {Subject}",
                email, subject);
            return Task.CompletedTask;
        }
    }
}
