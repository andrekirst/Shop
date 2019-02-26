using Polly;
using RabbitMQ.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Messaging
{
    public class RabbitMQMessagePublisher : IMessagePublisher
    {
        public RabbitMQMessagePublisher(string hostname, string username, string password, string exchange)
        {
            Hostname = hostname;
            Username = username;
            Password = password;
            Exchange = exchange;
        }

        public string Hostname { get; }

        public string Username { get; }

        public string Password { get; }

        public string Exchange { get; }

        public Task PublishMessageAsync(string messageType, object message, string routingKey)
        {
            return Task.Run(() =>
                Policy
                    .Handle<Exception>()
                    .WaitAndRetry(9, r => TimeSpan.FromSeconds(5), (ex, ts) => { Log.Error("Error connecting to RabbitMQ. Retrying in 5 sec."); })
                    .Execute(() =>
                    {
                        var factory = new ConnectionFactory
                        {
                            HostName = Hostname,
                            UserName = Username,
                            Password = Password
                        };
                        using (var connection = factory.CreateConnection())
                        {
                            using (var model = connection.CreateModel())
                            {
                                //model.ExchangeDeclare(
                                //    exchange: Exchange,
                                //    type: "fanout",
                                //    durable: true,
                                //    autoDelete: false);
                                model.QueueDeclare(queue: "test", durable: true, exclusive: false, autoDelete: false, arguments: null);
                                string data = MessageSerializer.Serialize(message);
                                var body = Encoding.UTF8.GetBytes(data);
                                IBasicProperties properties = model.CreateBasicProperties();
                                properties.Headers = new Dictionary<string, object> { { "MessageType", messageType } };
                                properties.Persistent = true;
                                model.BasicPublish(
                                    exchange: Exchange,
                                    routingKey: routingKey,
                                    basicProperties: properties,
                                    body: body);
                            }
                        }
                    })
            );
        }
    }
}
