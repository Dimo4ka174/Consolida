using Consolida.Infrastructure.Email;
using DB;
using DB.Auth;
using DB.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Consolida.Extensions
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// DbContext + ASP.NET Core Identity с кастомным ApplicationUser.
        /// </summary>
        public static IServiceCollection AddCustomDatabaseAndIdentity(
            this IServiceCollection services,
            IConfiguration configuration)
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

        /// <summary>
        /// Настройки cookie аутентификации.
        /// </summary>
        public static IServiceCollection AddCustomAuthentication(
            this IServiceCollection services)
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

                // SameAsRequest — работает и по HTTP, и по HTTPS.
                // Always — сломает логин при заходе на http://localhost:5041,
                // потому что cookie не будет сохраняться.
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.ReturnUrlParameter = "returnUrl";
            });

            return services;
        }

        /// <summary>
        /// Регистрация permission-политик из DB.Authorization.
        /// </summary>
        public static IServiceCollection AddCustomAuthorization(
            this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                AuthorizationPolicies.Configure(options);
            });

            return services;
        }

        /// <summary>
        /// MVC + Razor Pages.
        /// </summary>
        public static IServiceCollection AddCustomControllers(
            this IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();
            return services;
        }
    }
}
