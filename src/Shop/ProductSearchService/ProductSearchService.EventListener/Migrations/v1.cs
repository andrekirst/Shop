using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Polly;
using ProductSearchService.EventListener.DataAccess;
using ProductSearchService.EventListener.Model;
using System;

namespace ProductSearchService.API.Migrations
{
    [DbContext(contextType: typeof(ProductSearchDbContext))]
    [Migration(id: "v1")]
    public class v1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(retryCount: 5, sleepDurationProvider: r => TimeSpan.FromSeconds(value: 5))
                .Execute(action: () =>
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
                            table.PrimaryKey(name: "PK_Products_ProductId", columns: column => column.ProductId);
                            table.UniqueConstraint(name: "UNIQUE_Products_Productnumber", columns: column => column.Productnumber);
                        });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(retryCount: 5, sleepDurationProvider: r => TimeSpan.FromSeconds(value: 5))
                .Execute(action: () =>
                {
                    migrationBuilder.DropTable(name: "Products");
                });
        }

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
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

            base.BuildTargetModel(modelBuilder: modelBuilder);
        }
    }
}
