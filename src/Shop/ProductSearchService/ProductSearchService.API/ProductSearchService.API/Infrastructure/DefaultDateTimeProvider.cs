using System;

namespace ProductSearchService.API.Infrastructure
{
    public class DefaultDateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
