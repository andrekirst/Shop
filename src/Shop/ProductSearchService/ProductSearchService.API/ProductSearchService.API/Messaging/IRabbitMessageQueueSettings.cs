namespace ProductSearchService.API.Messaging
{
    public interface IRabbitMessageQueueSettings
    {
        string HostName { get; }

        string UserName { get; }

        string Password { get; }
    }
}
