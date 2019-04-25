using System;

namespace ProductSearchService.API.Caching
{
    public interface ICache
    {
        void Set<T>(string key, T value, TimeSpan? duration = null);

        T Get<T>(string key);

        void Update<T>(string key, Action<T> action, TimeSpan? duration = null);
    }
}
