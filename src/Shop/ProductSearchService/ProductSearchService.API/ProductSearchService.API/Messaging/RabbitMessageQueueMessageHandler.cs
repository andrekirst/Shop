using FluentTimeSpan;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductSearchService.API.Messaging
{
    public class RabbitMessageQueueMessageHandler : IMessageHandler
    {
        private const string MessageType = "MessageType";
        private IMessageHandlerCallback _callback;
        private IConnection _connection;
        private IModel _channel;
        private AsyncEventingBasicConsumer _consumer;
        private string _consumerTag;

        public RabbitMessageQueueMessageHandler(
            IRabbitMessageQueueSettings settings,
            string exchange,
            string queue,
            string routingKey,
            IMessageSerializer messageSerializer,
            ILogger<RabbitMessageQueueMessageHandler> logger)
        {
            Settings = settings;
            Exchange = exchange;
            Queue = queue;
            RoutingKey = routingKey;
            MessageSerializer = messageSerializer;
            Logger = logger;
        }

        public IRabbitMessageQueueSettings Settings { get; }
        
        private string Exchange { get; }

        private string Queue { get; }

        private string RoutingKey { get; }
        
        private IMessageSerializer MessageSerializer { get; }
        
        private ILogger<RabbitMessageQueueMessageHandler> Logger { get; }

        public void Start(IMessageHandlerCallback callback)
        {
            _callback = callback;

            Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    retryCount: 20,
                    sleepDurationProvider: r => 5.Seconds(),
                    onRetry: (ex, ts) =>
                    {
                        Logger.LogError(
                            exception: ex,
                            message: "Error connecting to RabbitMQ. Retrying in 5 sec.");
                    })
                .Execute(action: () =>
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = Settings.HostName,
                        UserName = Settings.UserName,
                        Password = Settings.Password,
                        DispatchConsumersAsync = true
                    };

                    var arguments = new Dictionary<string, object>()
                        {
                            { MessageType, Queue }
                        };

                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    _channel.ExchangeDeclare(
                        exchange: Exchange,
                        type: ExchangeType.Topic,
                        durable: true);
                    _channel.QueueDeclare(
                        queue: Queue,
                        durable: true,
                        autoDelete: false,
                        exclusive: false,
                        arguments: arguments);
                    _channel.QueueBind(
                        queue: Queue,
                        exchange: Exchange,
                        routingKey: RoutingKey,
                        arguments: arguments);
                    _consumer = new AsyncEventingBasicConsumer(model: _channel);
                    _consumer.Received += Consumer_Received;
                    _consumerTag = _channel.BasicConsume(
                        queue: Queue,
                        autoAck: false,
                        consumer: _consumer);
                });
        }

        public void Stop()
        {
            _channel.BasicCancel(consumerTag: _consumerTag);
            _channel.Close(replyCode: 200, replyText: "Goodbye");
            _connection.Close(reasonCode: 200, reasonText: "Goodbye");
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            if (await HandleEvent(@event: @event))
            {
                _channel.BasicAck(deliveryTag: @event.DeliveryTag, multiple: false);
            }
            else
            {
                _channel.BasicReject(deliveryTag: @event.DeliveryTag, requeue: false);
            }
        }

        private Task<bool> HandleEvent(BasicDeliverEventArgs @event)
        {
            string messageType = MessageSerializer.Encoding.GetString(bytes: (byte[])@event.BasicProperties.Headers[key: MessageType]);
            string body = MessageSerializer.Encoding.GetString(bytes: @event.Body);

            return _callback.HandleMessageAsync(messageType: messageType, message: body);
        }
    }
}
