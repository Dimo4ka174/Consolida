using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Consolida.Infrastructure.Logging;
using Consolida.Extensions;
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
    .AddCustomControllers();

// Регистрация всех прикладных сервисов через ServiceFactory
var serviceFactory = new ServiceFactory();
serviceFactory.RegisterServices(builder.Services);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Инициализация БД и сидирование
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

app.Run();
