using System;

namespace ProductSearchService.API.Infrastructure
{
    public class DefaultCorrelationIdGenerator : ICorrelationIdGenerator
    {
        public string Generate()
            => Guid.NewGuid().ToString().ToLower();
    }
}
