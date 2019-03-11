namespace ProductSearchService.EventListener.Messaging
{
    public interface IMessageHandler
    {
        void Start(IMessageHandlerCallback callback);

        void Stop();
    }
}