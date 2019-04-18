namespace ProductSearchService.API.Messaging
{
    public interface IMessageHandler<TCallback>
        where TCallback : IMessageHandlerCallback
    {
        void Start(TCallback callback);

        void Stop();
    }
}
