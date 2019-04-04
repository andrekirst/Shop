using ProductSearchService.EventListener.Events;
using ProductSearchService.EventListener.Messaging;
using ProductSearchService.EventListener.Model;
using ProductSearchService.EventListener.Repositories;
using Serilog;
using ServiceStack.Redis;
using System.Threading.Tasks;

namespace ProductSearchService.EventListener.EventHandlers
{
    public class ProductNameChangedEventHandler : IMessageHandlerCallback
    {
        public ProductNameChangedEventHandler(IMessageHandler messageHandler, IProductsRepository repository, IMessageSerializer messageSerializer)
        {
            MessageHandler = messageHandler;
            Repository = repository;
            MessageSerializer = messageSerializer;
        }

        public IMessageHandler MessageHandler { get; }

        public IProductsRepository Repository { get; }

        public IMessageSerializer MessageSerializer { get; }

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
                var redisManager = new RedisManagerPool("localhost:6379");
                var redis = redisManager.GetClient();
                Product product = redis.Get<Product>(@event.Productnumber);
                if (product != null)
                {
                    product.Name = @event.Name;
                    redis.Set(@event.Productnumber, product); 
                }
            }

            return successfulUpdated;
        }
    }
}
