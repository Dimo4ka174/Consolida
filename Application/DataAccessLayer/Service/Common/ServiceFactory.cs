using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Service.Entity;
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
                RegisterAutoMapper(services);
                RegisterDataAccess(services);
                RegisterBusinessServices(services);
                RegisterDBInitService(services);

                Log.Information("All services registered successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to register services");
                throw;
            }
        }

        private static void RegisterAutoMapper(IServiceCollection services)
        {
            services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());
        }

        private static void RegisterDataAccess(IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
            services.AddScoped(typeof(IFilterService<>), typeof(FilterService<>));
            services.AddScoped<ICascadeSoftDeleteService, CascadeSoftDeleteService>();
        }

        private static void RegisterBusinessServices(IServiceCollection services)
        {
            services.AddScoped<ICityService, CityService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<ICodeTNVDService, CodeTNVDService>();
            services.AddScoped<IManufacturerService, ManufacturerService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IProductApiService, ProductApiService>();
            services.AddScoped<IMeasureUnitService, MeasureUnitService>();
            services.AddScoped<ITaxTypeService, TaxTypeService>();
        }

        private static void RegisterDBInitService(IServiceCollection services)
        {
            services.AddScoped<IDBInitializer, DBInitializer>();
        }
    }
}
