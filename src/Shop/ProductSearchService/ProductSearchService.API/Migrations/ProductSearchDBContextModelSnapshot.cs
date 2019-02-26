using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Polly;
using ProductSearchService.API.DataAccess;
using ProductSearchService.API.Model;
using System;

namespace ProductSearchService.API.Migrations
{
    [DbContext(typeof(ProductSearchDbContext))]
    public class ProductSearchDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(5, r => TimeSpan.FromSeconds(5))
                .Execute(() =>
                {
                    modelBuilder.HasAnnotation("Version", "1.0");

                    modelBuilder.Entity<Product>(b =>
                    {
                        b.Property(p => p.ProductId).ValueGeneratedOnAdd();
                        b.Property(p => p.Productnumber);
                        b.Property(p => p.Name);
                        b.Property(p => p.Description);
                        b.ToTable("Products");
                    });
                });
        }
    }
}
