using Application.DataAccessLayer.Interface.Common;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Application.DataAccessLayer.Service.Common
{
    public class ServiceFactory
    {
        public void RegisterServices(IServiceCollection services)
        {
            Log.Information("Starting service registration");

            try
            {
                RegisterDBInitService(services);

                Log.Information("All services registered successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to register services");
                throw;
            }
        }

        private static void RegisterDBInitService(IServiceCollection services)
        {
            services.AddScoped<IDBInitializer, DBInitializer>();
        }
    }
}
