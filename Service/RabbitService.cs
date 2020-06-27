using EdcsServer.Helper;
using EdcsServer.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace EdcsServer.Service
{
    public interface IRabbitService
    {
        void StartListening();
    }
    public class RabbitService : IRabbitService
    {
        private readonly string Host;
        private readonly string Username;
        private readonly string Password;
        private readonly string Port;
        private readonly string QueueName;

        private readonly IConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        private readonly IDbService _dbService;
        private readonly IModelHelper _modelHelper;
        public RabbitService(IConfiguration config, IDbService dbService, IModelHelper modelHelper)
        {
            Host = config["Rabbit:Host"];
            Username = config["Rabbit:Username"];
            Password = config["Rabbit:Password"];
            Port = config["Rabbit:Port"];
            QueueName = config["Rabbit:QueueName"];

            _dbService = dbService;
            _modelHelper = modelHelper;

            _factory = new ConnectionFactory()
            {
                HostName = Host,
                UserName = Username,
                Password = Password,
                Port = Convert.ToInt32(Port)
            };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

            StartListening();
        }
        private void ExchangeDeclare()
        {
            _channel.
                ExchangeDeclare(
                exchange: "logs",
                type: ExchangeType.Direct);

            _channel.
                ExchangeDeclare(
                exchange: "users",
                type: ExchangeType.Topic);
        }
        private string QueueDeclare()
        {
            var queueName = _channel.QueueDeclare().QueueName;
            _channel.QueueBind(
                queue: queueName,
                exchange: "logs",
                routingKey: "MsgLogs");

            return queueName;
        }
        private void UserQueueDeclare()
        {
            var users = _dbService.GetUsersIds();

            if (users.Count() > 0)
            {
                Console.WriteLine("Rejestruję kolejki użytkowników");
                foreach (var user in users)
                {
                    string routingKey = String.Format("user.{0}", user);
                    string queueName = String.Format("user-{0}", user);

                    _channel.QueueDeclare(
                        queue: queueName, 
                        exclusive: false, 
                        autoDelete: false);
                    _channel.QueueBind(
                        queue: queueName,
                        exchange: "users",
                        routingKey: routingKey);
                }
            }
            else
                Console.WriteLine("Brak użytkowników");
        }
        public void StartListening()
        {
            ExchangeDeclare();
            var qName = QueueDeclare();
            UserQueueDeclare();

            var consumer = new EventingBasicConsumer(_channel);
            _channel.BasicConsume(
                queue: qName,
                autoAck: false,
                consumer: consumer);

            consumer.Received += (model, ea) =>
            {
                string response = String.Empty;
                var body = ea.Body.ToArray();
                var props = ea.BasicProperties;

                var replyProps = _channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;
                replyProps.Timestamp = ea.BasicProperties.Timestamp;
                Console.WriteLine("Received msg");
                try
                {
                    var msg = _modelHelper.DeserializeJson<Message>(Encoding.UTF8.GetString(body));
                    if (_dbService.SaveMessage(msg))
                    {
                        // TODO: loggowanie wiadomości
                        response = _modelHelper.SerializeJson(new Response
                        {
                            StatusCode = 200,
                            Message = "Message was saved to db."
                        });
                        Console.WriteLine("Message saved");

                    }
                    else
                    {
                        response = _modelHelper.SerializeJson(new Response
                        {
                            StatusCode = 501,
                            Message = "Message was not saved to db."
                        });
                        Console.WriteLine("Message not saved");
                    }
                }
                catch (ArgumentNullException ex)
                {
                    response = _modelHelper.SerializeJson(new Response
                    {
                        StatusCode = 502,
                        Message = "Message was not saved to db"
                    });
                    Console.WriteLine("Exc: Message saved");
                }
                catch (Exception ex)
                {
                    response = _modelHelper.SerializeJson(new Response
                    {
                        StatusCode = 502,
                        Message = "Message was not saved to db"
                    });
                    Console.WriteLine("Exc: Message saved");
                }
                finally
                {
                    var responseBody = Encoding.UTF8.GetBytes(response);

                    _channel.BasicPublish(
                        exchange: "",
                        routingKey: props.ReplyTo,
                        basicProperties: replyProps,
                        body: responseBody);

                    _channel.BasicAck(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false
                        );
                }
            };
        }
    }
}
