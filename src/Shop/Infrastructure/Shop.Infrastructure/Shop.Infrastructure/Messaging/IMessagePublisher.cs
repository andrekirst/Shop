using System.Threading.Tasks;

namespace Shop.Infrastructure.Messaging
{
    public interface IMessagePublisher
    {
        Task SendMessageAsync(object message, string messageType, string exchange);

        Task SendEventAsync(Event @event, string messageType, string exchange);
    }
}
