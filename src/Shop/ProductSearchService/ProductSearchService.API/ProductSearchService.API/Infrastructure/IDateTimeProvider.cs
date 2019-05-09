using System;

namespace ProductSearchService.API.Infrastructure
{
    public interface IDateTimeProvider
    {
        DateTime Now { get; }
    }
}
