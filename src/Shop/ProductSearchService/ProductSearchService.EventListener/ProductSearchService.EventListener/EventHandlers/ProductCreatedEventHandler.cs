using ProductSearchService.EventListener.Events;
using ProductSearchService.EventListener.Messaging;
using ProductSearchService.EventListener.Repositories;
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
            return messageType != "Event:ProductCreatedEvent"
                ? Task.FromResult(result: false)
                : HandleAsync(@event: MessageSerializer.Deserialize<ProductCreatedEvent>(value: message));
        }

        private Task<bool> HandleAsync(ProductCreatedEvent @event)
            => Repository.CreateProduct(
                productnumber: @event.Productnumber,
                name: @event.Name,
                description: @event.Description);

        public void Start() => MessageHandler.Start(callback: this);

        public void Stop() => MessageHandler.Stop();
        
        private IMessageHandler MessageHandler { get; }
        
        private IProductsRepository Repository { get; }
        
        private IMessageSerializer MessageSerializer { get; }
    }
}
