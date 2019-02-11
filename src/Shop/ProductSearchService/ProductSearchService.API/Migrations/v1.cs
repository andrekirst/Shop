using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProductSearchService.API.DataAccess;
using ProductSearchService.API.Model;

namespace ProductSearchService.API.Migrations
{
    [DbContext(typeof(ProductSearchDbContext))]
    [Migration("v1")]
    public class v1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<long>(nullable: false),
                    Productnumber = table.Column<string>(nullable: false, maxLength: 256),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(name: "PK_Products_ProductId", column => column.ProductId);
                    table.UniqueConstraint("UNIQUE_Products_Productnumber", column => column.Productnumber);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Products");
        }

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Version", "1.0");

            modelBuilder.Entity<Product>(b =>
            {
                b.Property(p => p.ProductId);
                b.Property(p => p.Productnumber);
                b.Property(p => p.Name);
                b.Property(p => p.Description);

                b.ToTable("Products");
            });
        }
    }
}
