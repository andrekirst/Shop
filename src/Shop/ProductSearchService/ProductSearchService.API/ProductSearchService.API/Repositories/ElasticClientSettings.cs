using Microsoft.Extensions.Configuration;

namespace ProductSearchService.API.Repositories
{
    public class ElasticClientSettings : IElasticClientSettings
    {
        private const string ElasticSection = "Elastic";

        public ElasticClientSettings(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public string Uri =>
            Configuration
            .GetSection(key: ElasticSection)
            .GetValue<string>(key: nameof(Uri));

        public int RequestTimoutInMinutes =>
            Configuration
            .GetSection(key: ElasticSection)
            .GetValue(
                key: nameof(RequestTimoutInMinutes),
                defaultValue: 2);

        public bool EnableHttpCompression =>
            Configuration
            .GetSection(key: ElasticSection)
            .GetValue(
                key: nameof(EnableHttpCompression),
                defaultValue: true);

        public bool EnableHttpPipelining =>
            Configuration
            .GetSection(key: ElasticSection)
            .GetValue(
                key: nameof(EnableHttpPipelining),
                defaultValue: true);

        public bool PrettyJson =>
            Configuration
            .GetSection(key: ElasticSection)
            .GetValue(
                key: nameof(PrettyJson),
                defaultValue: true);
    }
}
