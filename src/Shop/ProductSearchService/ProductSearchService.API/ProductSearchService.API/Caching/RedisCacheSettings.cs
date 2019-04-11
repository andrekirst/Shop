using Microsoft.Extensions.Configuration;

namespace ProductSearchService.API.Caching
{
    public class RedisCacheSettings : IRedisCacheSettings
    {
        public RedisCacheSettings(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public string Host =>
            Configuration.GetSection("Redis")["Host"] ?? "localhost:6379";

        private IConfiguration Configuration { get; }
    }
}
