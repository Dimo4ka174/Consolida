using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Application.DataAccessLayer.Jobs;
using Hangfire.PostgreSql;
using Hangfire;

namespace Application.DataAccessLayer.Extensions
{
    public static class HangfireExtensions
    {
        public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Регистрируем Hangfire server
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                    options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))));

            services.AddHangfireServer();

            // Регистрируем джобы
            services.AddScoped<DeliveryNotificationJob>();
            services.AddScoped<LockCleanupJob>();

            return services;
        }
    }
}
