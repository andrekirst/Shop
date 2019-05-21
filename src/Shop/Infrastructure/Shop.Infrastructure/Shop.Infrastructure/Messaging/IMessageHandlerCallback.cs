using System.Threading.Tasks;

namespace Shop.Infrastructure.Messaging
{
    public interface IMessageHandlerCallback
    {
        Task<bool> HandleMessageAsync(string messageType, string message);

        void Start();

        void Stop();
    }
}
