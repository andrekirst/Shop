using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductSearchService.API.Messaging
{
    public class RabbitMessageQueueMessagePublisher : IMessagePublisher, IDisposable
    {
        private IConnection _connection;
        private IModel _channel;
        private readonly IMessageSerializer _messageSerializer;
        private readonly ILogger<RabbitMessageQueueMessagePublisher> _logger;

        public RabbitMessageQueueMessagePublisher(string hostname, string username, string password, string exchange, IMessageSerializer messageSerializer, ILogger<RabbitMessageQueueMessagePublisher> logger)
        {
            Hostname = hostname;
            Username = username;
            Password = password;
            Exchange = exchange;
            _messageSerializer = messageSerializer;
            _logger = logger;

            CreateChannel();
        }

        public string Hostname { get; }

        public string Username { get; }

        public string Password { get; }

        public string Exchange { get; }

        public Task SendMessageAsync(object message, string messageType)
        {
            return Task.Run(action: () =>
                Policy
                    .Handle<Exception>()
                    .WaitAndRetry(
                        retryCount: 9,
                        sleepDurationProvider: r => TimeSpan.FromSeconds(value: 5),
                        onRetry: (ex, ts) =>
                        {
                            _logger.LogError(exception: ex, message: "Error connecting to RabbitMQ. Retrying in 5 sec.");
                        })
                    .Execute(action: () =>
                    {
                        BasicProperties publishProperties = CreateProperties(message: message, messageType: messageType);
                        string data = _messageSerializer.Serialize(value: message);
                        var body = _messageSerializer.Encoding.GetBytes(s: data);
                        PublishMessage(
                            publishProperties: publishProperties,
                            message: body);

                        if (message is Event @event)
                        {
                            _logger.LogInformation(eventId: @event.EventId, message: @event.ToString()); 
                        }
                    })
            );
        }

        private void PublishMessage(BasicProperties publishProperties, byte[] message)
        {
            _channel.BasicPublish(
                exchange: Exchange,
                routingKey: "",
                basicProperties: publishProperties,
                body: message);
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
                ContentType = _messageSerializer.ContentType,
                ContentEncoding = _messageSerializer.Encoding.WebName,
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
                HostName = Hostname,
                UserName = Username,
                Password = Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            CreateExchange();
        }

        private void CreateExchange()
        {
            _channel.ExchangeDeclare(
                exchange: Exchange,
                type: ExchangeType.Headers,
                durable: true,
                autoDelete: false,
                arguments: null);
        }

        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
        }
    }
}
