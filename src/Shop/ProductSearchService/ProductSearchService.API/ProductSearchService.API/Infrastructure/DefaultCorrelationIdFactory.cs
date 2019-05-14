using static System.Guid;

namespace ProductSearchService.API.Infrastructure
{
    public class DefaultCorrelationIdFactory : ICorrelationIdFactory
    {
        public string Build()
            => NewGuid()
                .ToString()
                .ToLower();
    }
}
