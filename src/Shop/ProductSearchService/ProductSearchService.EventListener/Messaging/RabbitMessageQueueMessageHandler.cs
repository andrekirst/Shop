using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System;
using System.Text;
using System.Threading.Tasks;
using FluentTimeSpan;

namespace ProductSearchService.EventListener.Messaging
{
    public class RabbitMessageQueueMessageHandler : IMessageHandler
    {
        private IMessageHandlerCallback _callback;
        private IConnection _connection;
        private IModel _channel;
        private AsyncEventingBasicConsumer _consumer;
        private string _consumerTag;

        public RabbitMessageQueueMessageHandler(string hostname, string username, string password, string exchange, string queue, string routingKey)
        {
            Hostname = hostname;
            Username = username;
            Password = password;
            Exchange = exchange;
            Queue = queue;
            RoutingKey = routingKey;
        }

        private string Hostname { get; }

        private string Username { get; }

        private string Password { get; }

        private string Exchange { get; }

        private string Queue { get; }

        private string RoutingKey { get; }

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
                        Log.Error(messageTemplate: "Error connecting to RabbitMQ. Retrying in 5 sec.");
                    })
                .Execute(action: () =>
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = Hostname,
                        UserName = Username,
                        Password = Password,
                        DispatchConsumersAsync = true
                    };

                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    _channel.ExchangeDeclare(exchange: Exchange, type: ExchangeType.Headers, durable: true);
                    _channel.QueueDeclare(queue: Queue, durable: true, autoDelete: false, exclusive: false);
                    _channel.QueueBind(queue: Queue, exchange: Exchange, routingKey: RoutingKey);
                    _consumer = new AsyncEventingBasicConsumer(model: _channel);
                    _consumer.Received += Consumer_Received;
                    _consumerTag = _channel.BasicConsume(queue: Queue, autoAck: false, consumer: _consumer);
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
            // TODO IMessageSerializer
            string messageType = Encoding.UTF8.GetString(bytes: (byte[])@event.BasicProperties.Headers[key: "MessageType"]);
            string body = Encoding.UTF8.GetString(bytes: @event.Body);

            return _callback.HandleMessageAsync(messageType: messageType, message: body);
        }
    }
}
