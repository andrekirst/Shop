using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProductSearchService.API.DataAccess;
using ProductSearchService.API.Model;

namespace ProductSearchService.API.Migrations
{
    [DbContext(typeof(ProductSearchDbContext))]
    public class ProductSearchDBContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Version", "1.0");

            modelBuilder.Entity<Product>(b =>
            {
                b.Property<long>(p => p.ProductId).ValueGeneratedOnAdd();
                b.Property<string>(p => p.Productnumber);
                b.Property<string>(p => p.Name);
                b.Property<string>(p => p.Description);

                b.ToTable("Products");
            });
        }
    }
}
