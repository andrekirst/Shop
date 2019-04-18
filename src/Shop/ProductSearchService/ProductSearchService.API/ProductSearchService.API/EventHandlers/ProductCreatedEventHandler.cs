using System.Threading.Tasks;
using FluentTimeSpan;
using ProductSearchService.API.Caching;
using ProductSearchService.API.Events;
using ProductSearchService.API.Messaging;
using ProductSearchService.API.Model;
using ProductSearchService.API.Repositories;

namespace ProductSearchService.API.EventHandlers
{
    public class ProductCreatedEventHandler : IMessageHandlerCallback
    {
        public ProductCreatedEventHandler(
            IMessageHandler<ProductCreatedEventHandler> messageHandler,
            IProductsRepository repository,
            IMessageSerializer messageSerializer,
            ICache<Product> productCache)
        {
            MessageHandler = messageHandler;
            Repository = repository;
            MessageSerializer = messageSerializer;
            ProductCache = productCache;
        }

        public void Start() => MessageHandler.Start(callback: this);

        public void Stop() => MessageHandler.Stop();

        private IMessageHandler<ProductCreatedEventHandler> MessageHandler { get; }

        private IProductsRepository Repository { get; }

        private IMessageSerializer MessageSerializer { get; }
        public ICache<Product> ProductCache { get; }

        public Task<bool> HandleMessageAsync(string messageType, string message)
        {
            return messageType == "Event:ProductCreatedEvent"
                ? HandleAsync(@event: MessageSerializer.Deserialize<ProductCreatedEvent>(value: message))
                : Task.FromResult(result: false);
        }

        private async Task<bool> HandleAsync(ProductCreatedEvent @event)
        {
            bool createdSuccessfully = await Repository.CreateProduct(
                productnumber: @event.Productnumber,
                name: @event.Name,
                description: @event.Description);

            if (createdSuccessfully)
            {
                ProductCache.Set(
                    key: @event.Productnumber,
                    value: new Product
                    {
                        Productnumber = @event.Productnumber,
                        Name = @event.Name,
                        Description = @event.Description
                    },
                    duration: 1.Hours());
            }

            return createdSuccessfully;
        }
    }
}
