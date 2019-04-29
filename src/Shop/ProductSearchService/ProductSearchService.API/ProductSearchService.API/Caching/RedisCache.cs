using System;
using FluentTimeSpan;
using Microsoft.Extensions.Logging;
using Polly;
using ServiceStack.Redis;

namespace ProductSearchService.API.Caching
{
    public class RedisCache : ICache
    {
        public RedisCache(
            ILogger<RedisCache> logger,
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
                        Logger.LogError(exception: ex, message: $"RedisCache: Redis initialization failed to Host {Settings.Host}");
                    })
                    .Execute(() =>
                    {
                        var redisManager = new RedisManagerPool(host: Settings.Host);
                        RedisClient = redisManager.GetClient();
                    });
        }

        private ILogger<RedisCache> Logger { get; }

        private IRedisCacheSettings Settings { get; }

        private IRedisClient RedisClient { get; set; }

        public T Get<T>(string key)
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    retryCount: 3,
                    sleepDurationProvider: r => 1.Seconds(),
                    onRetry: (ex, ts) =>
                    {
                        Logger.LogError(exception: ex, message: $"RedisCache: Get key \"{key}\"");
                    })
                .Execute(() =>
                {
                    Logger.LogInformation($"RedisCache: Begin get data for key \"{key}\"");
                    var item = RedisClient.Get<T>(key: key);
                    Logger.LogInformation($"RedisCache: End get data for key \"{key}\"");
                    return item;
                });
        }

        public void Set<T>(string key, T value, TimeSpan? duration = null)
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    retryCount: 3,
                    sleepDurationProvider: r => 1.Seconds(),
                    onRetry: (ex, ts) =>
                    {
                        Logger.LogError(exception: ex, message: $"RedisCache: Set key \"{key}\"");
                    })
                    .Execute(action: () =>
                    {
                        if (duration.HasValue)
                        {
                            RedisClient.Set(
                                key: key,
                                value: value,
                                expiresIn: duration.Value);
                            Logger.LogInformation(message: $"RedisCache: Cache key \"{key}\" for duration: {duration.Value}");
                        }
                        else
                        {
                            RedisClient.Set(
                                key: key,
                                value: value);
                            Logger.LogInformation(message: $"RedisCache: Cache key \"{key}\"");
                        }
                    });
        }

        public void Update<T>(string key, Action<T> action, TimeSpan? duration = null)
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    retryCount: 3,
                    sleepDurationProvider: r => 1.Seconds(),
                    onRetry: (ex, ts) =>
                    {
                        Logger.LogError(exception: ex, message: $"RedisCache: Update key \"{key}\"");
                    })
                    .Execute(action: () =>
                    {
                        Logger.LogInformation(message: $"RedisCache: Get data for key \"{key}\" to update");
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
                                Logger.LogInformation(message: $"RedisCache: Set data for key \"{key}\" to update with duration {duration.Value}");
                            }
                            else
                            {
                                RedisClient.Set(
                                    key: key,
                                    value: item);
                                Logger.LogInformation(message: $"RedisCache: Set data for key \"{key}\" to update");
                            }
                        }
                    });
        }
    }
}
