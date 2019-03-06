using System.Threading.Tasks;

namespace ProductSearchService.API.Messaging
{
    public interface IMessagePublisher
    {
        Task SendMessageAsync(object message, string messageType);
    }
}
