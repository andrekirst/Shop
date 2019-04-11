using System;

namespace ProductSearchService.API.Caching
{
    public interface ICache<T>
    {
        void Set(string key, T value, TimeSpan? duration = null);

        T Get(string key);

        void Update(string key, Action<T> action, TimeSpan? duration = null);
    }
}
