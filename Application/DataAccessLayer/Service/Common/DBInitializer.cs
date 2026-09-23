using Application.DataAccessLayer.Interface.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DB.Authorization;
using DB.DataSeeder;
using Npgsql;
using DB;

namespace Application.DataAccessLayer.Service.Common
{
    public class DBInitializer : IDBInitializer
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DBInitializer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public DBInitializer(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<DBInitializer> logger,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting database initialization");

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogCritical("Connection string 'DefaultConnection' is not configured");
                throw new InvalidOperationException("Connection string is not configured");
            }

            try
            {
                await WaitForDatabase(connectionString, cancellationToken);
                await EnsureDatabaseExists(connectionString);

                _logger.LogInformation("Applying database migrations");
                await _context.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Migrations applied successfully");

                // Всегда пытаемся засеять. Каждый сидер сам решает,
                // нужно ли ему работать (проверка Any() внутри).
                await SeedDatabase(cancellationToken);

                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization failed");
                throw;
            }
        }

        private async Task WaitForDatabase(string connectionString, CancellationToken cancellationToken)
        {
            var masterConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = "postgres"
            }.ConnectionString;

            for (int i = 1; i <= 10; i++)
            {
                try
                {
                    await using var connection = new NpgsqlConnection(masterConnectionString);
                    await connection.OpenAsync(cancellationToken);
                    _logger.LogInformation("DBMS connection is established");
                    return;
                }
                catch (Exception ex) when (i < 10)
                {
                    _logger.LogWarning(
                        "Database is unavailable (attempt {Attempt}/10): {Message}",
                        i, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                }
            }

            throw new TimeoutException("The database is unavailable after 10 attempts");
        }

        private async Task<bool> EnsureDatabaseExists(string connectionString)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                _logger.LogInformation("Database already exists");
                return false;
            }
            catch (NpgsqlException ex) when (ex.SqlState == "3D000")
            {
                _logger.LogInformation("Database does not exist, creating...");
                return await CreateDatabase(connectionString);
            }
        }

        private async Task<bool> CreateDatabase(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var targetDb = builder.Database;

            if (string.IsNullOrWhiteSpace(targetDb))
            {
                throw new InvalidOperationException(
                    "Database name is not specified in the connection string.");
            }

            var masterConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = "postgres"
            }.ConnectionString;

            await using var masterConn = new NpgsqlConnection(masterConnectionString);
            await masterConn.OpenAsync();

            // Проверяем race condition
            using var checkCmd = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @dbName", masterConn);
            checkCmd.Parameters.AddWithValue("dbName", targetDb);

            var exists = await checkCmd.ExecuteScalarAsync();
            if (exists != null)
            {
                _logger.LogInformation("The database already exists (race condition)");
                return false;
            }

            using var createCmd = new NpgsqlCommand(
                $"CREATE DATABASE {targetDb} WITH OWNER = postgres ENCODING = 'UTF8' CONNECTION LIMIT = -1",
                masterConn);

            await createCmd.ExecuteNonQueryAsync();
            _logger.LogInformation("Database '{Db}' created successfully", targetDb);
            return true;
        }

        private async Task SeedDatabase(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var scopedServices = scope.ServiceProvider;

            var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();
            var dbContext = scopedServices.GetRequiredService<AppDbContext>();

            _logger.LogInformation("Running database seeders...");
            try
            {
                await DataSeeder.SeedAsync(dbContext, userManager, roleManager, _logger);
                _logger.LogInformation("Database seeding completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database seeding failed");
                throw;
            }
        }
    }
}
