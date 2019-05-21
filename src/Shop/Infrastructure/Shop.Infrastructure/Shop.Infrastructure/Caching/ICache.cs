using System;

namespace Shop.Infrastructure.Caching
{
    public interface ICache
    {
        void Set<T>(string key, T value, TimeSpan? duration = null);

        T Get<T>(string key)
            where T : class;

        void Update<T>(string key, Action<T> action, TimeSpan? duration = null);
    }
}
