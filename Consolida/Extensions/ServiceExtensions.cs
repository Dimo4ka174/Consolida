using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.DataAccessLayer.CacheService;
using Microsoft.AspNetCore.DataProtection;
using Consolida.Infrastructure.Email;
using Consolida.Infrastructure.Redis;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.OpenApi;
using DB.Authorization;
using DB.Entity;
using DB.Auth;
using DB;

namespace Consolida.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddCustomDatabaseAndIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is not configured.");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString,
                    assembly => assembly.MigrationsAssembly("DB")));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;

                options.User.RequireUniqueEmail = false;
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>,
                               CustomUserClaimsPrincipalFactory>();

            services.AddTransient<IEmailSender, LoggingEmailSender>();

            return services;
        }

        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services)
        {
            services.ConfigureApplicationCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.Cookie.MaxAge = TimeSpan.FromDays(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Name = "Consolida.Auth";
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.ReturnUrlParameter = "returnUrl";
            });

            return services;
        }

        public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                AuthorizationPolicies.Configure(options);
            });

            return services;
        }

        public static IServiceCollection AddCustomDataProtection(this IServiceCollection services, IConfiguration configuration)
        {
            var isContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            var path = configuration["DATA_PROTECTION_PATH"]
                ?? (isContainer ? "/app/DataProtection-Keys" : "DataProtection-Keys");

            var dir = new DirectoryInfo(path);
            if (!dir.Exists)
                dir.Create();

            services.AddDataProtection()
                .PersistKeysToFileSystem(dir)
                .SetApplicationName("Consolida")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

            return services;
        }

        public static IServiceCollection AddCustomControllers(this IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();
            return services;
        }

        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Consolida API",
                    Version = "v1",
                    Description = "Справочники и заказы"
                });
            });

            return services;
        }

        public static IServiceCollection AddCustomCache(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();

            // HttpClient для CurrencyCacheService (запрос курса ЦБ РФ)
            services.AddSingleton<HttpClient>();

            var redisHost = configuration["REDIS_HOST"];

            if (!string.IsNullOrEmpty(redisHost))
            {
                services.Configure<RedisOptions>(options =>
                {
                    options.Host = redisHost;
                    options.Port = configuration.GetValue("REDIS_PORT", 6379);
                    options.Password = configuration["REDIS_PASSWORD"];
                    options.InstanceName = configuration["REDIS_INSTANCE_NAME"] ?? "Consolida_";
                });

                services.AddSingleton<RedisConnectionService>();

                services.AddStackExchangeRedisCache(options =>
                {
                    options.ConnectionMultiplexerFactory = () =>
                    {
                        var sp = services.BuildServiceProvider();
                        var svc = sp.GetRequiredService<RedisConnectionService>();
                        return Task.FromResult<IConnectionMultiplexer>(svc.Connection);
                    };
                    options.InstanceName = configuration["REDIS_INSTANCE_NAME"] ?? "Consolida_";
                });

                services.AddScoped(typeof(ICacheStrategy<>), typeof(RedisCacheStrategy<>));
                services.AddScoped<ICurrencyCacheService, RedisCurrencyCacheService>();
            }
            else
            {
                services.AddScoped(typeof(ICacheStrategy<>), typeof(MemoryCacheStrategy<>));
                services.AddScoped<ICurrencyCacheService, MemoryCurrencyCacheService>();
            }

            // Точечные регистрации кэш-сервисов-обёрток.
            services.AddScoped<ICacheService<City>, CityCacheService>();
            services.AddScoped<ICacheService<Company>, CompanyCacheService>();
            services.AddScoped<ICacheService<Manufacturer>, ManufacturerCacheService>();
            services.AddScoped<ICacheService<MeasureUnit>, MeasureUnitCacheService>();
            services.AddScoped<ICacheService<TaxType>, TaxTypeCacheService>();

            // Enum cache (in-memory, потому что зависит только от самого enum)
            services.AddScoped<IEnumCacheService, EnumCacheService>();

            return services;
        }
    }
}
