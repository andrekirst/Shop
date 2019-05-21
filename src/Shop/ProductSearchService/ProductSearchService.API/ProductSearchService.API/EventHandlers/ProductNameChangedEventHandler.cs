using System;
using System.Threading;
using System.Threading.Tasks;
using FluentTimeSpan;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Infrastructure.Caching;
using ProductSearchService.API.Events;
using ProductSearchService.API.Exceptions;
using ProductSearchService.API.Hubs;
using Shop.Infrastructure.Messaging;
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
                : throw new WrongMessageTypeGivenException(
                    expectedMessageType: "Event:ProductNameChangedEvent",
                    currentMessageType: messageType);
        }

        public void Start() => MessageHandler.Start(callback: this);

        public void Stop() => MessageHandler.Stop();

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Logger.LogDebug(message: $"Worker running at: {DateTimeOffset.Now}");
                await Task.Delay(delay: 1.Minutes(), cancellationToken: stoppingToken);
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
                    action: (product) =>
                    {
                        product.Name = @event.Name;
                    },
                    duration: 24.Hours());
            }

            return successfulUpdated;
        }

        private void PublishProductNameUpdated(ProductNameChangedEvent @event) =>
            _ = Task.Factory.StartNew(function: () => ProductHubContext.Clients.All.SendAsync(method: $"UpdateProductName[Productnumber={@event.Productnumber}]", arg1: @event.Name));
    }
}
