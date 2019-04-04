using ProductSearchService.EventListener.Events;
using ProductSearchService.EventListener.Messaging;
using ProductSearchService.EventListener.Model;
using ProductSearchService.EventListener.Repositories;
using ServiceStack.Redis;
using System.Threading.Tasks;

namespace ProductSearchService.EventListener.EventHandlers
{
    public class ProductCreatedEventHandler : IMessageHandlerCallback
    {
        public ProductCreatedEventHandler(IMessageHandler messageHandler, IProductsRepository repository, IMessageSerializer messageSerializer)
        {
            MessageHandler = messageHandler;
            Repository = repository;
            MessageSerializer = messageSerializer;
        }

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
                var redisManager = new RedisManagerPool("localhost:6379");
                var redis = redisManager.GetClient();
                var redisProducts = redis.As<Product>();
                Product product = new Product
                {
                    Productnumber = @event.Productnumber,
                    Name = @event.Name,
                    Description = @event.Description
                };
                redisProducts.SetValue(@event.Productnumber, product);
            }

            return createdSuccessfully;
        }

        public void Start() => MessageHandler.Start(callback: this);

        public void Stop() => MessageHandler.Stop();

        private IMessageHandler MessageHandler { get; }

        private IProductsRepository Repository { get; }

        private IMessageSerializer MessageSerializer { get; }
    }
}
