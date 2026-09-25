using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DB.Authorization;
using DB.Converters;
using DB.Abstract;
using DB.Entity;

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

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderProduct> OrdersProducts { get; set; }
        public DbSet<OrderTax> OrdersTaxes { get; set; }
        public DbSet<OrderTaxProduct> OrdersTaxProducts { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistory { get; set; }
        public DbSet<MetrologicalInfo> MetrologicalInfos { get; set; }

        public DbSet<OrderNotification> OrderNotifications { get; set; }
        public DbSet<ConsolidationPool> ConsolidationPools { get; set; }
        public DbSet<ConsolidationWeightLimit> ConsolidationWeightLimits { get; set; }
        public DbSet<ConsolidationPoolHistory> ConsolidationPoolHistories { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            // Все DateTime конвертируются в UTC при записи
            // и помечаются Kind=Utc при чтении.
            configurationBuilder.Properties<DateTime>()
                .HaveConversion<UtcDateTimeConverter>();

            configurationBuilder.Properties<DateTime?>()
                .HaveConversion<NullableUtcDateTimeConverter>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Enum'ы как строки ---
            modelBuilder.Entity<Company>().Property(e => e.Country).HasConversion<string>();
            modelBuilder.Entity<Customer>().Property(e => e.PreferredMethod).HasConversion<string>();
            modelBuilder.Entity<Order>().Property(e => e.Priority).HasConversion<string>();
            modelBuilder.Entity<Order>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<OrderStatusHistory>().Property(e => e.OldStatus).HasConversion<string>();
            modelBuilder.Entity<OrderStatusHistory>().Property(e => e.NewStatus).HasConversion<string>();
            modelBuilder.Entity<ConsolidationPool>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<ConsolidationPoolHistory>().Property(e => e.EventType).HasConversion<string>();

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

            // --- Order ---
            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.OrderNumber, o.IsDeleted })
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CreationDate);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CustomerId);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Order / ConsolidationPool ---
            modelBuilder.Entity<Order>()
                .HasOne(o => o.ConsolidationPool)
                .WithMany(p => p.Orders)
                .HasForeignKey(o => o.ConsolidationPoolId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.ConsolidationPoolId);

            // --- OrderProduct ---
            modelBuilder.Entity<OrderProduct>()
                .HasOne(op => op.Order)
                .WithMany(o => o.OrdersProducts)
                .HasForeignKey(op => op.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderProduct>()
                .HasOne(op => op.Product)
                .WithMany(p => p.OrdersProducts)
                .HasForeignKey(op => op.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- OrderTax ---
            modelBuilder.Entity<OrderTax>()
                .HasOne(ot => ot.Order)
                .WithMany(o => o.OrderTaxes)
                .HasForeignKey(ot => ot.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderTax>()
                .HasOne(ot => ot.TaxType)
                .WithMany()
                .HasForeignKey(ot => ot.TaxTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- OrderTaxProduct ---
            modelBuilder.Entity<OrderTaxProduct>()
                .HasOne(otp => otp.Order)
                .WithMany(o => o.OrderTaxProduct)
                .HasForeignKey(otp => otp.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderTaxProduct>()
                .HasOne(otp => otp.OrderProduct)
                .WithMany(op => op.OrdersTaxes)
                .HasForeignKey(otp => otp.OrderProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderTaxProduct>()
                .HasOne(otp => otp.TaxType)
                .WithMany(tt => tt.OrdersTax)
                .HasForeignKey(otp => otp.TaxTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- OrderStatusHistory ---
            modelBuilder.Entity<OrderStatusHistory>()
                .HasOne(osh => osh.Order)
                .WithMany(o => o.StatusHistory)
                .HasForeignKey(osh => osh.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- MetrologicalInfo ---
            modelBuilder.Entity<MetrologicalInfo>()
                .HasOne(mi => mi.Order)
                .WithOne(o => o.MetrologicalInfo)
                .HasForeignKey<MetrologicalInfo>(mi => mi.OrderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MetrologicalInfo>()
                .HasOne(mi => mi.OrderProduct)
                .WithOne(op => op.MetrologicalInfo)
                .HasForeignKey<MetrologicalInfo>(mi => mi.OrderProductId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // --- OrderNotification ---
            modelBuilder.Entity<OrderNotification>()
                .HasOne(n => n.Order)
                .WithMany(o => o.Notifications)
                .HasForeignKey(n => n.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderNotification>()
                .HasIndex(n => new { n.OrderId, n.IsCompleted, n.IsDeleted });

            modelBuilder.Entity<OrderNotification>()
                .HasIndex(n => n.DueDate);

            // --- ConsolidationPool ---
            // Optimistic concurrency через системную колонку Postgres xmin.
            modelBuilder.Entity<ConsolidationPool>()
                .Property(p => p.Version)
                .IsRowVersion();

            modelBuilder.Entity<ConsolidationPool>()
                .HasIndex(p => p.Status);

            modelBuilder.Entity<ConsolidationPool>()
                .HasIndex(p => p.IsDeleted);

            // --- ConsolidationWeightLimit ---
            modelBuilder.Entity<ConsolidationWeightLimit>()
                .HasIndex(l => new { l.Value, l.IsDeleted });

            // --- ConsolidationPoolHistory ---
            modelBuilder.Entity<ConsolidationPoolHistory>()
                .HasIndex(h => h.PoolId);
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
