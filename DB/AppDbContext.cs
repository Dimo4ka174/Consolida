using DB.Abstract;
using DB.Authorization;
using DB.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DB
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<City> Cities { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Customer> Customers { get; set; }

        public DbSet<CodeTNVD> CodesTNVD { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<MeasureUnit> MeasureUnits { get; set; }
        public DbSet<TaxType> TaxTypes { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Identity
            base.OnModelCreating(modelBuilder);

            // --- Enum'ы как строки ---
            modelBuilder.Entity<Company>()
                .Property(e => e.Country)
                .HasConversion<string>();

            modelBuilder.Entity<Customer>()
                .Property(e => e.PreferredMethod)
                .HasConversion<string>();

            // --- City / Company / Customer ---
            modelBuilder.Entity<City>()
                .HasMany(c => c.Companies)
                .WithOne(c => c.City)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Company>()
                .HasMany(c => c.Customers)
                .WithOne(c => c.Company)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- CodeTNVD ---
            modelBuilder.Entity<CodeTNVD>()
                .HasIndex(c => new { c.Name, c.IsDeleted })
                .IsUnique();

            // --- Product / Manufacturer ---
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Manufacturer)
                .WithMany(m => m.Products)
                .HasForeignKey(p => p.ManufacturerId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Product / CodeTNVD ---
            modelBuilder.Entity<Product>()
                .HasOne(p => p.CodeTNVD)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CodeTNVDId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- TaxType / MeasureUnit ---
            modelBuilder.Entity<TaxType>()
                .HasOne(t => t.MeasureUnit)
                .WithMany(m => m.TaxTypes)
                .HasForeignKey(t => t.MeasureUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public override int SaveChanges()
        {
            ApplyAuditTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditTimestamps()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<ICreatedAtEntity>())
            {
                if (entry.State == EntityState.Added && entry.Entity.CreatedDate == default)
                {
                    entry.Entity.CreatedDate = now;
                }
            }
        }
    }
}
