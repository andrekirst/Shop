using System.Threading.Tasks;
using FluentTimeSpan;
using Microsoft.AspNetCore.SignalR;
using ProductSearchService.API.Caching;
using ProductSearchService.API.Events;
using ProductSearchService.API.Hubs;
using ProductSearchService.API.Messaging;
using ProductSearchService.API.Model;
using ProductSearchService.API.Repositories;

namespace ProductSearchService.API.EventHandlers
{
    public class ProductNameChangedEventHandler : IMessageHandlerCallback
    {
        public ProductNameChangedEventHandler(
            IMessageHandler<ProductNameChangedEventHandler> messageHandler,
            IProductsRepository repository,
            IMessageSerializer messageSerializer,
            ICache<Product> productCache,
            IHubContext<ProductHub> productHubContext)
        {
            MessageHandler = messageHandler;
            Repository = repository;
            MessageSerializer = messageSerializer;
            ProductCache = productCache;
            ProductHubContext = productHubContext;
        }

        private IMessageHandler<ProductNameChangedEventHandler> MessageHandler { get; }

        private IProductsRepository Repository { get; }

        private IMessageSerializer MessageSerializer { get; }

        private ICache<Product> ProductCache { get; }
        
        private IHubContext<ProductHub> ProductHubContext { get; }

        public Task<bool> HandleMessageAsync(string messageType, string message)
        {
            return messageType == "Event:ProductNameChangedEvent"
                ? HandleAsync(@event: MessageSerializer.Deserialize<ProductNameChangedEvent>(value: message))
                : Task.FromResult(false);
        }

        public void Start() => MessageHandler.Start(callback: this);

        public void Stop() => MessageHandler.Stop();

        private async Task<bool> HandleAsync(ProductNameChangedEvent @event)
        {
            bool successfulUpdated = await Repository.UpdateProductName(
                productnumber: @event.Productnumber,
                name: @event.Name);

            await ProductHubContext.Clients.All.SendAsync(
                method: $"UpdateProductName[Productnumber={@event.Productnumber}]",
                arg1: @event.Productnumber,
                arg2: @event.Name);

            if (successfulUpdated)
            {
                var product = ProductCache.Get(@event.Productnumber);

                if (product != null)
                {
                    product.Name = @event.Name;

                    ProductCache.Set(
                        key: @event.Productnumber,
                        value: product,
                        duration: 24.Hours());
                }
            }

            return successfulUpdated;
        }
    }
}
