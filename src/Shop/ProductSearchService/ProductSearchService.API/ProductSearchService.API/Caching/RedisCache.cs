using System;
using FluentTimeSpan;
using Microsoft.Extensions.Logging;
using Polly;
using ServiceStack.Redis;

namespace ProductSearchService.API.Caching
{
    public class RedisCache<T> : ICache<T>
    {
        public RedisCache(
            ILogger<RedisCache<T>> logger,
            IRedisCacheSettings settings)
        {
            Logger = logger;
            Settings = settings;
            InitializeRedis();
        }

        private void InitializeRedis()
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    retryCount: 3,
                    sleepDurationProvider: r => 1.Seconds(),
                    onRetry: (ex, ts) =>
                    {
                        Logger.LogError(exception: ex, message: $"Redis initialization failed to Host {Settings.Host}");
                    })
                    .Execute(() =>
                    {
                        var redisManager = new RedisManagerPool(host: Settings.Host);
                        RedisClient = redisManager.GetClient();
                    });
        }

        private ILogger<RedisCache<T>> Logger { get; }

        public IRedisCacheSettings Settings { get; }

        public IRedisClient RedisClient { get; private set; }

        public T Get(string key)
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetry(
                retryCount: 3,
                sleepDurationProvider: r => 1.Seconds(),
                onRetry: (ex, ts) =>
                {
                    Logger.LogError(exception: ex, message: $"Get key \"{key}\"");
                })
                .Execute(() =>
                {
                    Logger.LogInformation($"Get data for key \"{key}\"");
                    return RedisClient.Get<T>(key: key);
                });
        }

        public void Set(string key, T value, TimeSpan? duration = null)
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    retryCount: 3,
                    sleepDurationProvider: r => 1.Seconds(),
                    onRetry: (ex, ts) =>
                    {
                        Logger.LogError(exception: ex, message: $"Set key \"{key}\"");
                    })
                    .Execute(action: () =>
                    {
                        if (duration.HasValue)
                        {
                            RedisClient.Set(
                                key: key,
                                value: value,
                                expiresIn: duration.Value);
                            Logger.LogInformation(message: $"Cache key \"{key}\" for duration: {duration.Value}");
                        }
                        else
                        {
                            RedisClient.Set(
                                key: key,
                                value: value); Logger.LogInformation(message: $"Cache key \"{key}\"");
                        }
                    });
        }

        public void Update(string key, Action<T> action, TimeSpan? duration = null)
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    retryCount: 3,
                    sleepDurationProvider: r => 1.Seconds(),
                    onRetry: (ex, ts) =>
                    {
                        Logger.LogError(exception: ex, message: $"Update key \"{key}\"");
                    })
                    .Execute(action: () =>
                    {
                        Logger.LogInformation(message: $"Lock key \"{key}\" for update");
                        using (RedisClient.AcquireLock(key: key, timeOut: 1.Seconds()))
                        {
                            Logger.LogInformation(message: $"Get data for key \"{key}\" to update");
                            T item = RedisClient.Get<T>(key: key);
                            if (item != null)
                            {
                                action?.Invoke(item);
                                if (duration.HasValue)
                                {
                                    RedisClient.Set(
                                        key: key,
                                        value: item,
                                        expiresIn: duration.Value);
                                    Logger.LogInformation(message: $"Set data for key \"{key}\" to update with duration {duration.Value}");
                                }
                                else
                                {
                                    RedisClient.Set(
                                        key: key,
                                        value: item); Logger.LogInformation(message: $"Set data for key \"{key}\" to update");
                                }
                            }
                        }
                    });
        }
    }
}
