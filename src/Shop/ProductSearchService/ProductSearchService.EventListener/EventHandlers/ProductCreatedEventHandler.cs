using ProductSearchService.EventListener.Events;
using ProductSearchService.EventListener.Messaging;
using ProductSearchService.EventListener.Repositories;
using System.Threading.Tasks;

namespace ProductSearchService.EventListener.EventHandlers
{
    public class ProductCreatedEventHandler : IMessageHandlerCallback
    {
        private readonly IMessageHandler _messageHandler;
        private readonly IProductsRepository _repository;
        private readonly IMessageSerializer _messageSerializer;

        public ProductCreatedEventHandler(IMessageHandler messageHandler, IProductsRepository repository, IMessageSerializer messageSerializer)
        {
            _messageHandler = messageHandler;
            _repository = repository;
            _messageSerializer = messageSerializer;
        }

        public async Task<bool> HandleMessageAsync(string messageType, string message)
        {
            if (messageType != "Event:ProductCreatedEvent")
            {
                return false;
            }

            return await HandleAsync(@event: _messageSerializer.Deserialize<ProductCreatedEvent>(value: message));
        }

        private Task<bool> HandleAsync(ProductCreatedEvent @event)
            => _repository.CreateProduct(
                productnumber: @event.Productnumber,
                name: @event.Name,
                description: @event.Description);

        public void Start() => _messageHandler.Start(callback: this);

        public void Stop() => _messageHandler.Stop();
    }
}
