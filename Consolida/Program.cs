using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.DataAccessLayer.Service.Hubs;
using Application.DataAccessLayer.Extensions;
using Application.DataAccessLayer.Jobs;
using Consolida.Infrastructure.Logging;
using Consolida.Extensions;
using Hangfire;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

SerilogConfigurator.ConfigureSerilog(builder.Configuration, builder.Host);

builder.Services
    .AddCustomDatabaseAndIdentity(builder.Configuration)
    .AddCustomDataProtection(builder.Configuration)
    .AddCustomAuthentication()
    .AddCustomAuthorization()
    .AddCustomControllers()
    .AddCustomSwagger()
    .AddCustomCache(builder.Configuration);

builder.Services.AddHangfireServices(builder.Configuration);

builder.Services.AddSignalR();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "Consolida.Session";
});

builder.Services.AddHttpContextAccessor();

var serviceFactory = new ServiceFactory();
serviceFactory.RegisterServices(builder.Services);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() },
    DashboardTitle = "Consolida Jobs"
});

app.UseSession();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ConsolidationHub>("/hubs/consolidation");

try
{
    using var scope = app.Services.CreateScope();
    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDBInitializer>();
    await dbInitializer.Initialize();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
    throw;
}

// Recurring jobs
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    recurringJobManager.AddOrUpdate<DeliveryNotificationJob>(
        "delivery-notification",
        job => job.ExecuteAsync(),
        Cron.Daily(9, 0), // каждый день в 09:00
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

    recurringJobManager.AddOrUpdate<LockCleanupJob>(
        "lock-cleanup",
        job => job.ExecuteAsync(),
        "*/15 * * * *", // каждые 15 минут
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
}

app.Run();
