using Microsoft.Extensions.Configuration;

namespace ProductSearchService.API.Messaging
{
    public class RabbitMessageQueueSettings : IRabbitMessageQueueSettings
    {
        public RabbitMessageQueueSettings(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public string HostName =>
            Configuration.GetSection(key: "RabbitMQ")["Hostname"];

        public string UserName =>
            Configuration.GetSection(key: "RabbitMQ")["Username"];

        public string Password =>
            Configuration.GetSection(key: "RabbitMQ")["Password"];

        public IConfiguration Configuration { get; }
    }
}
