using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Messaging
{
    public class RabbitMQMessageHandler : IMessageHandler
    {
        private IMessageHandlerCallback _callback;
        private IConnection _connection;
        private IModel _model;
        private AsyncEventingBasicConsumer _consumer;
        private string _consumerTag;

        public RabbitMQMessageHandler(string hostname, string username, string password, string exchange, string queuename, string routingKey)
        {
            Hostname = hostname;
            Username = username;
            Password = password;
            Exchange = exchange;
            Queuename = queuename;
            RoutingKey = routingKey;
        }

        public string Hostname { get; }

        public string Username { get; }

        public string Password { get; }

        public string Exchange { get; }

        public string Queuename { get; }

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
                    _model = _connection.CreateModel();
                    _model.ExchangeDeclare(
                        exchange: Exchange,
                        type: "fanout",
                        durable: true,
                        autoDelete: false);
                    _model.QueueDeclare(
                        queue: Queuename,
                        durable: true,
                        autoDelete: false,
                        exclusive: false);
                    _model.QueueBind(
                        queue: Queuename,
                        exchange: Exchange,
                        routingKey: RoutingKey);
                    _consumer = new AsyncEventingBasicConsumer(_model);
                    _consumer.Received += Consumer_Received;
                    _consumerTag = _model.BasicConsume(
                        queue: Queuename,
                        autoAck: false,
                        consumer: _consumer);
                });
        }

        public void Stop()
        {
            _model.BasicCancel(consumerTag: _consumerTag);
            _model.Close(replyCode: 200, replyText: "Goodbye");
            _connection.Close();
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            if (await HandleEvent(@event))
            {
                _model.BasicAck(
                    deliveryTag: @event.DeliveryTag,
                    multiple: false);
            }
        }

        private Task<bool> HandleEvent(BasicDeliverEventArgs @event)
        {
            string messageType = Encoding.UTF8.GetString((byte[])@event.BasicProperties.Headers["MessageType"]);

            string body = Encoding.UTF8.GetString(@event.Body);

            return _callback.HandleMessageAsync(
                messageType: messageType,
                message: body);
        }
    }
}
