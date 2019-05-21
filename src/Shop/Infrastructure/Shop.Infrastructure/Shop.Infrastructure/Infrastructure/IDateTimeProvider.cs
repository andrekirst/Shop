using System;

namespace Shop.Infrastructure.Infrastructure
{
    public interface IDateTimeProvider
    {
        DateTime Now { get; }
    }
}
