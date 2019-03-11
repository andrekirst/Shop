using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Polly;
using ProductSearchService.EventListener.DataAccess;
using ProductSearchService.EventListener.EventHandlers;
using ProductSearchService.EventListener.Messaging;
using ProductSearchService.EventListener.Repositories;
using Serilog;
using System;
using System.IO;
using System.Threading;

namespace ProductSearchService.EventListener
{
    public class Program
    {
        public static string ShopEnvironment { get; private set; }

        public static IConfigurationRoot Config { get; private set; }

        static Program()
        {
            ShopEnvironment = Environment.GetEnvironmentVariable("SHOP_ENVIRONMENT") ?? "Development";

            Config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{ShopEnvironment}.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Config)
                .CreateLogger();

            Log.Information($"Environment: {ShopEnvironment}");
        }

        public static void Main(string[] args)
        {
            Startup();
        }

        private static void Startup()
        {
            var configSection = Config.GetSection("RabbitMQ");
            string hostname = configSection["Hostname"];
            string username = configSection["Username"];
            string password = configSection["Password"];

            RabbitMQMessageHandler messageHandlerProductCreated = new RabbitMQMessageHandler(
                hostname: hostname,
                username: username,
                password: password,
                exchange: "Product",
                queue: "Product:Event:ProductCreatedEvent",
                routingKey: "");

            string connectionString = ConnectionString;
            var dbContextOptions = new DbContextOptionsBuilder<ProductSearchDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            var dbContext = new ProductSearchDbContext(dbContextOptions);

            Policy
                .Handle<Exception>()
                .WaitAndRetry(5, r => TimeSpan.FromSeconds(5), (ex, ts) => { Log.Error("Error connecting to DB. Retrying in 5 sec."); })
                .Execute(() => dbContext.Database.Migrate());

            ProductsRepository repository = new ProductsRepository(dbContext);
            JsonMessageSerializer messageSerializer = new JsonMessageSerializer();

            ProductCreatedEventHandler productCreatedEventHandler = new ProductCreatedEventHandler(
                messageHandlerProductCreated,
                repository,
                messageSerializer);
            productCreatedEventHandler.Start();

            if (ShopEnvironment == "Development")
            {
                Log.Information("ProductSearchService.Eventhandler started.");
                Console.WriteLine("Press any key to stop...");
                Console.ReadKey(true);
                productCreatedEventHandler.Stop();
            }
            else
            {
                Log.Information("ProductSearchService.Eventhandler started.");
                while (true)
                {
                    Thread.Sleep(10000);
                }
            }
        }

        private static string ConnectionString => Config.GetConnectionString(name: "ProductSearchConnectionString");
    }
}
