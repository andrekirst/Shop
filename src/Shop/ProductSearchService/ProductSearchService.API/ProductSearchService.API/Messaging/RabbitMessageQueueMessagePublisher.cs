using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentTimeSpan;
using Microsoft.Extensions.Caching.Memory;

namespace ProductSearchService.API.Messaging
{
    public class RabbitMessageQueueMessagePublisher : IMessagePublisher, IDisposable
    {
        private IConnection _connection;
        private IModel _channel;

        public RabbitMessageQueueMessagePublisher(
            IRabbitMessageQueueSettings settings,
            IMessageSerializer messageSerializer,
            ILogger<RabbitMessageQueueMessagePublisher> logger,
            IMemoryCache memoryCache)
        {
            Settings = settings;
            MessageSerializer = messageSerializer;
            Logger = logger;
            MemoryCache = memoryCache;

            CreateChannel();
        }

        public IRabbitMessageQueueSettings Settings { get; }
        
        private IMessageSerializer MessageSerializer { get; }
        
        private ILogger<RabbitMessageQueueMessagePublisher> Logger { get; }
        private IMemoryCache MemoryCache { get; }

        public Task SendMessageAsync(object message, string messageType, string exchange) =>
            Task.Run(action: () =>
                Policy
                    .Handle<Exception>()
                    .WaitAndRetry(
                        retryCount: 20,
                        sleepDurationProvider: r => 5.Seconds(),
                        onRetry: (ex, ts) =>
                        {
                            Logger.LogError(exception: ex, message: "Error connecting to RabbitMQ. Retrying in 5 sec.");
                        })
                    .Execute(action: () =>
                    {
                        BasicProperties publishProperties = CreateProperties(message: message, messageType: messageType);
                        string data = MessageSerializer.Serialize(value: message);
                        byte[] body = MessageSerializer.Encoding.GetBytes(s: data);
                        PublishMessage(
                            publishProperties: publishProperties,
                            message: body,
                            exchange: exchange);

                        if (message is Event @event)
                        {
                            Logger.LogInformation(eventId: @event.EventId, message: @event.ToString()); 
                        }
                    }));

        public Task SendEventAsync(Event @event, string messageType, string exchange) =>
            Task.Run(action: () =>
                Policy
                    .Handle<Exception>()
                    .WaitAndRetry(
                        retryCount: 20,
                        sleepDurationProvider: r => 5.Seconds(),
                        onRetry: (ex, ts) =>
                        {
                            Logger.LogError(exception: ex, message: "Error connecting to RabbitMQ. Retrying in 5 sec.");
                        })
                    .Execute(action: () =>
                    {
                        BasicProperties publishProperties = CreateProperties(message: @event, messageType: messageType);
                        string data = MessageSerializer.Serialize(value: @event);
                        byte[] body = MessageSerializer.Encoding.GetBytes(s: data);
                        PublishMessage(
                            publishProperties: publishProperties,
                            message: body,
                            exchange: exchange);
                            Logger.LogInformation(eventId: @event.EventId, message: @event.ToString());
                    }));

        private void PublishMessage(BasicProperties publishProperties, byte[] message, string exchange)
        {
            if (!IsExchangeIsAlreadyDeclared(exchange))
            {
                CreateExchange(exchange: exchange);
            }

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: "",
                basicProperties: publishProperties,
                body: message);
        }

        private bool IsExchangeIsAlreadyDeclared(string exchange)
        {
            bool found =
                MemoryCache.TryGetValue(
                    key: $"{nameof(RabbitMessageQueueMessagePublisher)}:ExchangeDeclared:{exchange}",
                    out object value);
            return found && (bool)value;
        }

        private void MarkExchangeAsDeclared(string exchange)
        {
            MemoryCache.Set(
                key: $"{nameof(RabbitMessageQueueMessagePublisher)}:ExchangeDeclared:{exchange}",
                value: true,
                absoluteExpirationRelativeToNow: 1.Minutes());
        }

        private BasicProperties CreateProperties(object message, string messageType)
        {
            BasicProperties properties = new BasicProperties
            {
                Headers = new Dictionary<string, object>
                {
                    { "MessageType", $"Event:{messageType}" }
                },
                Persistent = true,
                ContentType = MessageSerializer.ContentType,
                ContentEncoding = MessageSerializer.Encoding.WebName,
                AppId = AppDomain.CurrentDomain.FriendlyName
            };

            if (message is Event @event)
            {
                properties.MessageId = @event.MessageId.ToString();
                properties.Timestamp = new AmqpTimestamp(unixTime: ((DateTimeOffset)@event.Timestamp).ToUnixTimeSeconds());
            }

            return properties;
        }

        private void CreateChannel()
        {
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = Settings.HostName,
                UserName = Settings.UserName,
                Password = Settings.Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        private void CreateExchange(string exchange)
        {
            _channel.ExchangeDeclare(
                exchange: exchange,
                type: ExchangeType.Headers,
                durable: true,
                autoDelete: false,
                arguments: null);

            MarkExchangeAsDeclared(exchange: exchange);
        }

        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
        }
    }
}
