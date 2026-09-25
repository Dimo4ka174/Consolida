using Application.DataAccessLayer.Interface.CalculationService;
using Application.DataAccessLayer.Interface.OrderService;
using Application.DataAccessLayer.Service.OrderService;
using Application.DataAccessLayer.Service.ExportExcel;
using Application.DataAccessLayer.Service.ImportExcel;
using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.Excel;
using Application.DataAccessLayer.Interface.Hubs;
using Application.DataAccessLayer.Service.Entity;
using Application.DataAccessLayer.Service.Hubs;
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
                RegisterReferenceServices(services);
                RegisterOrderServices(services);
                RegisterOrderApiServices(services);
                RegisterCalculationServices(services);
                RegisterExcelServices(services);
                RegisterNotificationServices(services);
                RegisterConsolidationServices(services);
                RegisterUtilities(services);
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

            // Инфраструктура в рамках DataAccess
            services.AddScoped<ILockService, LockService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IConsolidationNotifier, ConsolidationNotifier>();
        }

        private static void RegisterDBInitService(IServiceCollection services)
        {
            services.AddScoped<IDBInitializer, DBInitializer>();
        }

        private static void RegisterReferenceServices(IServiceCollection services)
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

        private static void RegisterOrderServices(IServiceCollection services)
        {
            services.AddScoped<IOrderListService, OrderListService>();
            services.AddScoped<IOrderDeleteService, OrderDeleteService>();
            services.AddScoped<IOrderCreationService, OrderCreationService>();
            services.AddScoped<IOrderDetailsService, OrderDetailsService>();
            services.AddScoped<IOrderDuplicateService, OrderDuplicateService>();
            services.AddScoped<IOrderSaveService, OrderSaveService>();
            services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();
        }

        private static void RegisterOrderApiServices(IServiceCollection services)
        {
            services.AddScoped<IOrderLookupService, OrderLookupService>();
            services.AddScoped<IOrderTaxManagementService, OrderTaxManagementService>();
            services.AddScoped<IOrderStatusService, OrderStatusService>();
            services.AddScoped<IQuickCreateService, QuickCreateService>();
        }

        private static void RegisterCalculationServices(IServiceCollection services)
        {
            services.AddScoped<ICalculationService, CalculationService>();
            services.AddScoped<ICalculationExportService, CalculationExportService>();
        }

        private static void RegisterExcelServices(IServiceCollection services)
        {
            services.AddScoped<IExcelService, ReadExcelResponseService>();
            services.AddScoped<IExcelOrderProcessingService, ExcelOrderProcessingService>();
            services.AddScoped<IExportExcelService, ExportExcelService>();
        }

        private static void RegisterNotificationServices(IServiceCollection services)
        {
            services.AddScoped<IOrderNotificationService, OrderNotificationService>();
        }

        private static void RegisterConsolidationServices(IServiceCollection services)
        {
            services.AddScoped<IConsolidationService, ConsolidationService>();
        }

        private static void RegisterUtilities(IServiceCollection services)
        {
            services.AddScoped<RussianNumberToWordsConverter>();
        }
    }
}
