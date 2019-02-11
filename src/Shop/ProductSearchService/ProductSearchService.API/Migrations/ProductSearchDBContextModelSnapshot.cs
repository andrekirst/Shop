using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProductSearchService.API.DataAccess;
using ProductSearchService.API.Model;

namespace ProductSearchService.API.Migrations
{
    [DbContext(typeof(ProductSearchDbContext))]
    public class ProductSearchDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
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
        }
    }
}
