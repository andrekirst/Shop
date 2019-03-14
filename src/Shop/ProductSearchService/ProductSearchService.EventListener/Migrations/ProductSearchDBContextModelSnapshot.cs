using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Polly;
using ProductSearchService.EventListener.DataAccess;
using ProductSearchService.EventListener.Model;
using System;

namespace ProductSearchService.EventListener.Migrations
{
    [DbContext(contextType: typeof(ProductSearchDbContext))]
    public class ProductSearchDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(retryCount: 5, sleepDurationProvider: r => TimeSpan.FromSeconds(value: 5))
                .Execute(action: () =>
                {
                    modelBuilder.HasAnnotation(annotation: "Version", value: "1.0");

                    modelBuilder.Entity<Product>(buildAction: b =>
                    {
                        b.Property(propertyExpression: p => p.ProductId).ValueGeneratedOnAdd();
                        b.Property(propertyExpression: p => p.Productnumber);
                        b.Property(propertyExpression: p => p.Name);
                        b.Property(propertyExpression: p => p.Description);

                        b.HasKey(keyExpression: p => p.ProductId);

                        b.ToTable(name: "Products");
                    });
                });
        }
    }
}
