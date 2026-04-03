using Cafe_Inventory_Management.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Cafe_Inventory_Management.Repository
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Product { get; set; }
        public DbSet<Ingredients> Ingredients { get; set; }
        public DbSet<OrdersModel> Orders { get; set; }
        public DbSet<OrderItems> OrderItems { get; set; }
        public DbSet<ProductIngredients> ProductIngredients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Force EF mapping to timestamptz to match PostgreSQL schema and avoid DateTime kind mismatch.
            modelBuilder.Entity<Product>().Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            modelBuilder.Entity<Product>().Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

            modelBuilder.Entity<Ingredients>().Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            modelBuilder.Entity<Ingredients>().Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

            modelBuilder.Entity<OrdersModel>().Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            modelBuilder.Entity<OrdersModel>().Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

            modelBuilder.Entity<OrderItems>().Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            modelBuilder.Entity<OrderItems>().Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

            modelBuilder.Entity<ProductIngredients>().Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            modelBuilder.Entity<ProductIngredients>().Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        }

        public override int SaveChanges()
        {
            NormalizeDateTimeKinds();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            NormalizeDateTimeKinds();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            NormalizeDateTimeKinds();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            NormalizeDateTimeKinds();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void NormalizeDateTimeKinds()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified);

            foreach (var entry in entries)
            {
                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType == typeof(DateTime))
                    {
                        var value = (DateTime?)property.CurrentValue;
                        if (value.HasValue)
                        {
                            property.CurrentValue = NormalizeToUtc(value.Value);
                        }
                    }
                    else if (property.Metadata.ClrType == typeof(DateTime?))
                    {
                        var value = (DateTime?)property.CurrentValue;
                        if (value.HasValue)
                        {
                            property.CurrentValue = NormalizeToUtc(value.Value);
                        }
                    }
                }
            }
        }

        private static DateTime NormalizeToUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
