using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DB.Authorization;
using DB.Abstract;
using DB.Entity;

namespace DB
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {

        public DbSet<City> Cities { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Customer> Customers { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Identity
            base.OnModelCreating(modelBuilder);

            // Конфигурация enum'ов (храним как строки)
            modelBuilder.Entity<Company>()
                .Property(e => e.Country)
                .HasConversion<string>();

            modelBuilder.Entity<Customer>()
                .Property(e => e.PreferredMethod)
                .HasConversion<string>();

            // Настройка связей
            modelBuilder.Entity<City>()
                .HasMany(c => c.Companies)
                .WithOne(c => c.City)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Company>()
                .HasMany(c => c.Customers)
                .WithOne(c => c.Company)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
