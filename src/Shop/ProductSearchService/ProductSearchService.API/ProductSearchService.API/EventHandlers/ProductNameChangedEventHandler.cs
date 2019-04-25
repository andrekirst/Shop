using System;
using System.Threading;
using System.Threading.Tasks;
using FluentTimeSpan;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductSearchService.API.Caching;
using ProductSearchService.API.Events;
using ProductSearchService.API.Hubs;
using ProductSearchService.API.Messaging;
using ProductSearchService.API.Model;
using ProductSearchService.API.Repositories;

namespace ProductSearchService.API.EventHandlers
{
    public class ProductNameChangedEventHandler : BackgroundService, IMessageHandlerCallback
    {
        public ProductNameChangedEventHandler(
            IMessageHandler<ProductNameChangedEventHandler> messageHandler,
            IProductsRepository repository,
            IMessageSerializer messageSerializer,
            ICache cache,
            IHubContext<ProductHub> productHubContext,
            ILogger<ProductNameChangedEventHandler> logger)
        {
            MessageHandler = messageHandler;
            Repository = repository;
            MessageSerializer = messageSerializer;
            Cache = cache;
            ProductHubContext = productHubContext;
            Logger = logger;
        }

        private IMessageHandler<ProductNameChangedEventHandler> MessageHandler { get; }

        private IProductsRepository Repository { get; }

        private IMessageSerializer MessageSerializer { get; }
        
        private ICache Cache { get; }
        
        private IHubContext<ProductHub> ProductHubContext { get; }
        
        private ILogger<ProductNameChangedEventHandler> Logger { get; }

        public Task<bool> HandleMessageAsync(string messageType, string message)
        {
            return messageType == "Event:ProductNameChangedEvent"
                ? HandleAsync(@event: MessageSerializer.Deserialize<ProductNameChangedEvent>(value: message))
                : Task.FromResult(false);
        }

        public void Start() => MessageHandler.Start(callback: this);

        public void Stop() => MessageHandler.Stop();

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Start();
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Stop();
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Logger.LogInformation(message: $"Worker running at: {DateTimeOffset.Now}");
                await Task.Delay(millisecondsDelay: 1000, cancellationToken: stoppingToken);
            }
        }

        private async Task<bool> HandleAsync(ProductNameChangedEvent @event)
        {
            string cacheKey = $"ProductSearchService.Product[Productnumber=\"{@event.Productnumber}\"]";
            bool successfulUpdated = await Repository.UpdateProductName(
                productnumber: @event.Productnumber,
                name: @event.Name);

            PublishProductNameUpdated(@event: @event);

            if (successfulUpdated)
            {
                Cache.Update<Product>(
                    key: cacheKey,
                    (product) =>
                    {
                        product.Name = @event.Name;
                    },
                    duration: 24.Hours());
            }

            return successfulUpdated;
        }

        private void PublishProductNameUpdated(ProductNameChangedEvent @event) =>
            _ = Task.Factory.StartNew(() => ProductHubContext.Clients.All.SendAsync($"UpdateProductName[Productnumber={@event.Productnumber}]", @event.Name));
    }
}
