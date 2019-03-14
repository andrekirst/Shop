using Microsoft.EntityFrameworkCore;
using ProductSearchService.API.Model;

namespace ProductSearchService.API.DataAccess
{
    public class ProductSearchDbContext : DbContext
    {
        public ProductSearchDbContext(DbContextOptions<ProductSearchDbContext> options)
            : base(options: options)
        {
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().HasKey(keyExpression: m => m.ProductId);
            modelBuilder.Entity<Product>().HasIndex(indexExpression: p => p.Productnumber).IsUnique();
            modelBuilder.Entity<Product>().ToTable(name: "Products");
        }
    }
}
