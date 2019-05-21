using System;
using System.Threading;
using System.Threading.Tasks;
using FluentTimeSpan;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Infrastructure.Caching;
using ProductSearchService.API.Events;
using ProductSearchService.API.Exceptions;
using Shop.Infrastructure.Messaging;
using ProductSearchService.API.Model;
using ProductSearchService.API.Repositories;

namespace ProductSearchService.API.EventHandlers
{
    public class ProductsCreatedEventHandler : BackgroundService, IMessageHandlerCallback
    {
        private const string MessageType = "Event:ProductsCreatedEvent";

        public ProductsCreatedEventHandler(
            ILogger<ProductsCreatedEventHandler> logger,
            IMessageHandler<ProductsCreatedEventHandler> messageHandler,
            IMessageSerializer messageSerializer,
            IProductsRepository repository,
            ICache cache)
        {
            Logger = logger;
            MessageHandler = messageHandler;
            MessageSerializer = messageSerializer;
            Repository = repository;
            Cache = cache;
        }

        private ILogger<ProductsCreatedEventHandler> Logger { get; }

        private IMessageHandler<ProductsCreatedEventHandler> MessageHandler { get; }

        private IMessageSerializer MessageSerializer { get; }
        
        private IProductsRepository Repository { get; }
        
        private ICache Cache { get; }

        public Task<bool> HandleMessageAsync(string messageType, string message)
        {
            return messageType == MessageType
                ? HandleAsync(@event: MessageSerializer.Deserialize<ProductsCreatedEvent>(value: message))
                : throw new WrongMessageTypeGivenException(
                    expectedMessageType: MessageType,
                    currentMessageType: messageType);
        }

        private async Task<bool> HandleAsync(ProductsCreatedEvent @event)
        {
            bool createdSuccessfully = await Repository.CreateProducts(
                products: @event.Products);

            if (createdSuccessfully)
            {
                foreach (CreateProductsItem product in @event.Products)
                {
                    string cacheKey = $"ProductSearchService.Product[Productnumber=\"{product.Productnumber}\"]";
                    Cache.Set(
                        key: cacheKey,
                        value: new Product
                        {
                            Productnumber = product.Productnumber,
                            Name = product.Name,
                            Description = product.Description
                        },
                        duration: 24.Hours());
                }
            }
            return createdSuccessfully;
        }

        public void Start() => MessageHandler.Start(callback: this);

        public void Stop() => MessageHandler.Stop();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Logger.LogDebug(message: $"Worker running at: {DateTimeOffset.Now}");
                await Task.Delay(delay: 1.Minutes(), cancellationToken: stoppingToken);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Start();
            return base.StartAsync(cancellationToken: cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Stop();
            return base.StopAsync(cancellationToken: cancellationToken);
        }
    }
}
