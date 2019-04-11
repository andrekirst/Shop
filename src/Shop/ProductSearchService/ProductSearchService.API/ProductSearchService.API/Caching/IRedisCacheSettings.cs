using System.Collections.Generic;

namespace ProductSearchService.API.Caching
{
    public interface IRedisCacheSettings
    {
        string Host { get; }
    }
}
