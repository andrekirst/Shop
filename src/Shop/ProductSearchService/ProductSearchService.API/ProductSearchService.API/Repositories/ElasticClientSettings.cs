using Microsoft.Extensions.Configuration;

namespace ProductSearchService.API.Repositories
{
    public class ElasticClientSettings : IElasticClientSettings
    {
        public ElasticClientSettings(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private IConfigurationSection ElasticSection =>
            Configuration.GetSection("Elastic");

        public string Uri =>
            ElasticSection
            .GetValue<string>(key: nameof(Uri));

        public int RequestTimoutInMinutes =>
            ElasticSection
            .GetValue(
                key: nameof(RequestTimoutInMinutes),
                defaultValue: 2);

        public bool EnableHttpCompression =>
            ElasticSection
            .GetValue(
                key: nameof(EnableHttpCompression),
                defaultValue: true);

        public bool EnableHttpPipelining =>
            ElasticSection
            .GetValue(
                key: nameof(EnableHttpPipelining),
                defaultValue: true);

        public bool PrettyJson =>
            ElasticSection
            .GetValue(
                key: nameof(PrettyJson),
                defaultValue: true);

        public int NumberOfShards =>
            ElasticSection
            .GetValue(
                key: nameof(NumberOfShards),
                defaultValue: 1);

        public int NumberOfReplicas =>
            ElasticSection
            .GetValue(
                key: nameof(NumberOfReplicas),
                defaultValue: 1);
    }
}
