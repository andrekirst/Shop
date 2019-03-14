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
using FluentTimeSpan;

namespace ProductSearchService.EventListener
{
    public class Program
    {
        public static string ShopEnvironment { get; private set; }

        public static IConfigurationRoot Config { get; private set; }

        static Program()
        {
            ShopEnvironment = Environment.GetEnvironmentVariable(variable: "SHOP_ENVIRONMENT") ?? "Development";

            Config = new ConfigurationBuilder()
                .SetBasePath(basePath: Directory.GetCurrentDirectory())
                .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(path: $"appsettings.{ShopEnvironment}.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration: Config)
                .CreateLogger();

            Log.Information(messageTemplate: $"Environment: {ShopEnvironment}");
        }

        public static void Main(string[] args)
        {
            Startup();
        }

        private static void Startup()
        {
            var configSection = Config.GetSection(key: "RabbitMQ");
            string hostname = configSection[key: "Hostname"];
            string username = configSection[key: "Username"];
            string password = configSection[key: "Password"];

            RabbitMessageQueueMessageHandler messageHandlerProductCreated = new RabbitMessageQueueMessageHandler(
                hostname: hostname,
                username: username,
                password: password,
                exchange: "Product",
                queue: "Product:Event:ProductCreatedEvent",
                routingKey: "");

            string connectionString = ConnectionString;
            var dbContextOptions = new DbContextOptionsBuilder<ProductSearchDbContext>()
                .UseSqlServer(connectionString: connectionString)
                .Options;

            var dbContext = new ProductSearchDbContext(options: dbContextOptions);

            Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    retryCount: 5,
                    sleepDurationProvider: r => 5.Seconds(),
                    onRetry: (ex, ts) => { Log.Error(messageTemplate: "Error connecting to DB. Retrying in 5 sec."); })
                .Execute(action: () => dbContext.Database.Migrate());

            ProductsRepository repository = new ProductsRepository(dbContext: dbContext);
            JsonMessageSerializer messageSerializer = new JsonMessageSerializer();

            ProductCreatedEventHandler productCreatedEventHandler = new ProductCreatedEventHandler(
                messageHandler: messageHandlerProductCreated,
                repository: repository,
                messageSerializer: messageSerializer);
            productCreatedEventHandler.Start();

            if (ShopEnvironment == "Development")
            {
                Log.Information(messageTemplate: "ProductSearchService.Eventhandler started.");
                Console.WriteLine(value: "Press any key to stop...");
                Console.ReadKey(intercept: true);
                productCreatedEventHandler.Stop();
            }
            else
            {
                Log.Information(messageTemplate: "ProductSearchService.Eventhandler started.");
                while (true)
                {
                    Thread.Sleep(millisecondsTimeout: 10000);
                }
            }
        }

        private static string ConnectionString => Config.GetConnectionString(name: "ProductSearchConnectionString");
    }
}
