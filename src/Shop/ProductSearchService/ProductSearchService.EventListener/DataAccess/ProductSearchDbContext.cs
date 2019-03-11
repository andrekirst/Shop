using Microsoft.EntityFrameworkCore;
using Polly;
using ProductSearchService.EventListener.Model;
using System;

namespace ProductSearchService.EventListener.DataAccess
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
            modelBuilder.Entity<Product>().HasKey(b => b.ProductId);
            modelBuilder.Entity<Product>().HasIndex(p => p.Productnumber).IsUnique();
            modelBuilder.Entity<Product>().ToTable("Products");

            base.OnModelCreating(modelBuilder);
        }

        public void MigrateDB()
        {
            Database.Migrate();
        }
    }
}
