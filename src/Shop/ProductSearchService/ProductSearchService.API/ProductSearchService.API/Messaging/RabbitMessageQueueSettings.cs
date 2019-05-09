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
            Configuration.GetSection(key: "RabbitMQ")[key: "Hostname"];

        public string UserName =>
            Configuration.GetSection(key: "RabbitMQ")[key: "Username"];

        public string Password =>
            Configuration.GetSection(key: "RabbitMQ")[key: "Password"];

        private IConfiguration Configuration { get; }
    }
}
