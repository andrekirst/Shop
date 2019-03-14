using Microsoft.EntityFrameworkCore;
using ProductSearchService.EventListener.Model;

namespace ProductSearchService.EventListener.DataAccess
{
    public class ProductSearchDbContext : DbContext
    {
        private readonly DbContextOptions<ProductSearchDbContext> _options;

        public ProductSearchDbContext(DbContextOptions<ProductSearchDbContext> options)
            : base(options: options)
        {
            _options = options;
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().HasKey(keyExpression: b => b.ProductId);
            modelBuilder.Entity<Product>().HasIndex(indexExpression: p => p.Productnumber).IsUnique();
            modelBuilder.Entity<Product>().ToTable(name: "Products");

            base.OnModelCreating(modelBuilder: modelBuilder);
        }

        public void MigrateDB()
        {
            Database.Migrate();
        }
    }
}
