using System.Threading.Tasks;

namespace ProductSearchService.EventListener.Messaging
{
    public interface IMessageHandlerCallback
    {
        Task<bool> HandleMessageAsync(string messageType, string message);

        void Start();

        void Stop();
    }
}