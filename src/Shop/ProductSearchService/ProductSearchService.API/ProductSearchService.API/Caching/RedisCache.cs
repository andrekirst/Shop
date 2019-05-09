using System;
using FluentTimeSpan;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using StackExchange.Redis;

namespace ProductSearchService.API.Caching
{
    public class RedisCache : ICache
    {
        public RedisCache(
            ILogger<RedisCache> logger,
            IRedisCacheSettings settings,
            IMemoryCache memoryCache)
        {
            Logger = logger;
            Settings = settings;
            MemoryCache = memoryCache;
            InitializeRedis();
        }

        private void InitializeRedis() =>
            Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    retryCount: 3,
                    sleepDurationProvider: r => 1.Seconds(),
                    onRetry: (ex, ts) =>
                    {
                        Logger.LogError(exception: ex, message: $"RedisCache: Redis initialization failed to Host {Settings.Host}");
                    })
                .Execute(action: () =>
                {
                    var redis = ConnectionMultiplexer.Connect(
                        configuration: $"{Settings.Host},name=ProductSearchService.API,connectRetry=1,responseTimeout=10000",
                        log: Console.Out);
                    redis.ErrorMessage += Redis_ErrorMessage;
                    redis.ConnectionFailed += Redis_ConnectionFailed;
                    redis.ConnectionRestored += Redis_ConnectionRestored;
                    redis.InternalError += Redis_InternalError;
                    Database = redis.GetDatabase();
                });

        private void Redis_InternalError(object sender, InternalErrorEventArgs e)
            => Logger.LogError(exception: e.Exception, message: e.Origin);

        private void Redis_ConnectionRestored(object sender, ConnectionFailedEventArgs e)
            => Logger.LogError(exception: e.Exception, message: e.Exception.Message);

        private void Redis_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
            => Logger.LogError(exception: e.Exception, message: e.Exception.Message);

        private void Redis_ErrorMessage(object sender, RedisErrorEventArgs e)
            => Logger.LogError(message: e.Message);

        private ILogger<RedisCache> Logger { get; }

        private IRedisCacheSettings Settings { get; }
        
        private IMemoryCache MemoryCache { get; }
        
        private IDatabase Database { get; set; }

        public T Get<T>(string key)
            where T : class
            => Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    retryCount: 3,
                    sleepDurationProvider: r => 1.Seconds(),
                    onRetry: (ex, ts) =>
                    {
                        Logger.LogError(exception: ex, message: $"RedisCache: Get key \"{key}\"");
                    })
                .Execute(action: () =>
                {
                    Logger.LogInformation(message: $"RedisCache: Begin get data for key \"{key}\"");
                    var item = MemoryCache.Get<T>(key: key) ?? GetFromRedis<T>(key: key);
                    Logger.LogInformation(message: $"RedisCache: End get data for key \"{key}\"");
                    return item;
                });

        public void Set<T>(string key, T value, TimeSpan? duration = null) =>
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
                        MemoryCache.Set(key: key, value: value, absoluteExpirationRelativeToNow: 30.Seconds());
                        SetToRedis(key: key, value: value, duration: duration);
                    });

        public void Update<T>(string key, Action<T> action, TimeSpan? duration = null) =>
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
                        T item = GetFromRedis<T>(key: key);
                        if (item != null)
                        {
                            action?.Invoke(obj: item);
                            MemoryCache.Set(key: key, value: item, absoluteExpirationRelativeToNow: 30.Seconds());
                            SetToRedis(
                                key: key,
                                value: item,
                                duration: duration);
                        }
                    });

        private T GetFromRedis<T>(string key)
        {
            string json = Database.StringGet(key: key);
            return json == null ? default : JsonConvert.DeserializeObject<T>(value: json);
        }

        private void SetToRedis<T>(string key, T value, TimeSpan? duration = null)
        {
            if (duration.HasValue)
            {
                Database.StringSet(
                    key: key,
                    value: JsonConvert.SerializeObject(value: value),
                    expiry: duration.Value);
                Logger.LogInformation(message: $"RedisCache: Cache key \"{key}\" for duration: {duration.Value}");
            }
            else
            {
                Database.StringSet(
                    key: key,
                    value: JsonConvert.SerializeObject(value: value));
                Logger.LogInformation(message: $"RedisCache: Cache key \"{key}\"");
            }
        }
    }
}
