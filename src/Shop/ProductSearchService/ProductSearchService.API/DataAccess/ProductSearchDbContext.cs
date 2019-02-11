using Microsoft.EntityFrameworkCore;
using Polly;
using ProductSearchService.API.Model;
using System;

namespace ProductSearchService.API.DataAccess
{
    public class ProductSearchDbContext : DbContext
    {
        private readonly DbContextOptions<ProductSearchDbContext> _options;

        public ProductSearchDbContext(DbContextOptions<ProductSearchDbContext> options)
            : base(options)
        {
            _options = options;
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().HasKey(m => m.ProductId);
            modelBuilder.Entity<Product>().HasIndex(p => p.Productnumber).IsUnique();
            modelBuilder.Entity<Product>().ToTable("Products");
        }

        public void MigrateDB()
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(5, r => TimeSpan.FromSeconds(5))
                .Execute(() =>
                {
                    Database.Migrate();
                });
        }
    }
}
