using System;

namespace Shop.Infrastructure.Infrastructure
{
    public class DefaultCorrelationIdFactory : ICorrelationIdFactory
    {
        public string Build()
            => Guid.NewGuid()
                .ToString()
                .ToLower();
    }
}
