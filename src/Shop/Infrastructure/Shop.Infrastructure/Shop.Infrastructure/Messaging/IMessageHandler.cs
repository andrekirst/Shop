namespace Shop.Infrastructure.Messaging
{
    public interface IMessageHandler<TCallback>
        where TCallback : IMessageHandlerCallback
    {
        void Start(TCallback callback);

        void Stop();
    }
}
