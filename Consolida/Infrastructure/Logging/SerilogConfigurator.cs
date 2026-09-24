using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Consolida.Infrastructure.Logging
{
    public static class SerilogConfigurator
    {
        public static void ConfigureSerilog(IConfiguration configuration, IHostBuilder hostBuilder)
        {
            // Настройка Serilog
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Debug() // Базовый уровень логирования - Debug
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information) // Microsoft - только Information и выше
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning) // ASP.NET Core - только Warning и выше
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning) // EF Core - только Warning и выше
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Consolida")
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
                .Enrich.With<ShortSourceContextEnricher>()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ShortSourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    Path.Combine("logs", "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ShortSourceContext}] {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 31,     // 31 день
                    fileSizeLimitBytes: 10_485_760, // 10Mb
                    rollOnFileSizeLimit: true)
                .WriteTo.File(
                    Path.Combine("logs", "error-.txt"),
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Error,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ShortSourceContext}] {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 31);

            // Дополнительные настройки для Docker окружения
            var isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            if (isRunningInContainer)
            {
                loggerConfiguration.WriteTo.Console(new CompactJsonFormatter()); // В Docker выводим JSON
            }

            Log.Logger = loggerConfiguration.CreateLogger();
            hostBuilder.UseSerilog();
        }
    }
}
