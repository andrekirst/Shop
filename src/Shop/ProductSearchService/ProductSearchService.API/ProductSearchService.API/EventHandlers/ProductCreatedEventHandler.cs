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
    public class ProductCreatedEventHandler : BackgroundService, IMessageHandlerCallback
    {
        private const string MessageType = "Event:ProductCreatedEvent";

        public ProductCreatedEventHandler(
            IMessageHandler<ProductCreatedEventHandler> messageHandler,
            IProductsRepository repository,
            IMessageSerializer messageSerializer,
            ICache cache,
            ILogger<ProductCreatedEventHandler> logger)
        {
            MessageHandler = messageHandler;
            Repository = repository;
            MessageSerializer = messageSerializer;
            Cache = cache;
            Logger = logger;
        }

        public void Start() => MessageHandler.Start(callback: this);

        public void Stop() => MessageHandler.Stop();

        private IMessageHandler<ProductCreatedEventHandler> MessageHandler { get; }

        private IProductsRepository Repository { get; }

        private IMessageSerializer MessageSerializer { get; }
        
        private ICache Cache { get; }
        
        private ILogger<ProductCreatedEventHandler> Logger { get; }

        public Task<bool> HandleMessageAsync(string messageType, string message)
        {
            return messageType == MessageType
                ? HandleAsync(@event: MessageSerializer.Deserialize<ProductCreatedEvent>(value: message))
                : throw new WrongMessageTypeGivenException(
                    expectedMessageType: MessageType,
                    currentMessageType: messageType);
        }

        private async Task<bool> HandleAsync(ProductCreatedEvent @event)
        {
            string cacheKey = $"ProductSearchService.Product[Productnumber=\"{@event.Productnumber}\"]";
            bool createdSuccessfully = await Repository.CreateProduct(
                productnumber: @event.Productnumber,
                name: @event.Name,
                description: @event.Description);

            if (createdSuccessfully)
            {
                Cache.Set(
                    key: cacheKey,
                    value: new Product
                    {
                        Productnumber = @event.Productnumber,
                        Name = @event.Name,
                        Description = @event.Description
                    },
                    duration: 24.Hours());
            }

            return createdSuccessfully;
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Logger.LogDebug(message: $"Worker {nameof(ProductCreatedEventHandler)} running at: {DateTimeOffset.Now}");
                await Task.Delay(delay: 1.Minutes(), cancellationToken: stoppingToken);
            }
        }
    }
}
