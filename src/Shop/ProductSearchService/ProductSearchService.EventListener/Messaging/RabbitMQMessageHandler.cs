using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ProductSearchService.EventListener.Messaging
{
    public class RabbitMQMessageHandler : IMessageHandler
    {
        private IMessageHandlerCallback _callback;
        private IConnection _connection;
        private IModel _channel;
        private AsyncEventingBasicConsumer _consumer;
        private string _consumerTag;

        public RabbitMQMessageHandler(string hostname, string username, string password, string exchange, string queue, string routingKey)
        {
            Hostname = hostname;
            Username = username;
            Password = password;
            Exchange = exchange;
            Queue = queue;
            RoutingKey = routingKey;
        }

        public string Hostname { get; }

        public string Username { get; }

        public string Password { get; }

        public string Exchange { get; }

        public string Queue { get; }

        public string RoutingKey { get; }

        public void Start(IMessageHandlerCallback callback)
        {
            _callback = callback;

            Policy
                .Handle<Exception>()
                .WaitAndRetry(9, r => TimeSpan.FromSeconds(5), (ex, ts) => { Log.Error("Error connecting to RabbitMQ. Retrying in 5 sec."); })
                .Execute(() =>
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
                    _channel.ExchangeDeclare(exchange: Exchange, type: ExchangeType.Headers, durable: true, autoDelete: false);
                    _channel.QueueDeclare(queue: Queue, durable: true, autoDelete: false, exclusive: false);
                    _channel.QueueBind(queue: Queue, exchange: Exchange, RoutingKey);
                    _consumer = new AsyncEventingBasicConsumer(_channel);
                    _consumer.Received += Consumer_Received;
                    _consumerTag = _channel.BasicConsume(queue: Queue, autoAck: false, consumer: _consumer);
                });
        }

        public void Stop()
        {
            _channel.BasicCancel(_consumerTag);
            _channel.Close(200, "Goodbye");
            _connection.Close(200, "Goodbye");
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            if (await HandleEvent(@event))
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
            string messageType = Encoding.UTF8.GetString(bytes: (byte[])@event.BasicProperties.Headers["MessageType"]);
            string body = Encoding.UTF8.GetString(bytes: @event.Body);

            return _callback.HandleMessageAsync(messageType: messageType, message: body);
        }
    }
}
