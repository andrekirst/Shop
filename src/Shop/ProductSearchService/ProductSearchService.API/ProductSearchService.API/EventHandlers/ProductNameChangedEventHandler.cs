using System.Threading.Tasks;
using FluentTimeSpan;
using ProductSearchService.API.Caching;
using ProductSearchService.API.Events;
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
            ICache<Product> productCache)
        {
            MessageHandler = messageHandler;
            Repository = repository;
            MessageSerializer = messageSerializer;
            ProductCache = productCache;
        }

        public IMessageHandler<ProductNameChangedEventHandler> MessageHandler { get; }

        public IProductsRepository Repository { get; }

        public IMessageSerializer MessageSerializer { get; }
        
        public ICache<Product> ProductCache { get; }

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
