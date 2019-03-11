using Newtonsoft.Json.Linq;
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

            JObject messageObject = _messageSerializer.Deserialize(message);

            return await HandleAsync(messageObject.ToObject<ProductCreatedEvent>());
        }

        private async Task<bool> HandleAsync(ProductCreatedEvent @event)
        {
            return await _repository.CreateProduct(@event.Productnumber, @event.Name, @event.Description);
        }

        public void Start()
        {
            _messageHandler.Start(this);
        }

        public void Stop()
        {
            _messageHandler.Stop();
        }
    }
}
