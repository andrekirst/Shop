using Microsoft.EntityFrameworkCore;
using Polly;
using ProductSearchService.API.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductSearchService.API.DataAccess
{
    public class ProductSearchDbContext : DbContext
    {
        public ProductSearchDbContext(DbContextOptions<ProductSearchDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().HasKey(m => m.Productnumber);
            base.OnModelCreating(modelBuilder);
        }

        public void MigrateDB()
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(5, r => TimeSpan.FromSeconds(5))
                .Execute(() => Database.Migrate());
        }
    }
}
