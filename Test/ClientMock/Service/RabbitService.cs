using EdcsClient.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace EdcsClient.Service
{
    public interface IRabbitService
    {
        void LogMessage(Message message);
        void SendMessageToUser(Message message);
    }
    public class RabbitService : IRabbitService
    {
        private readonly Settings _config;
        private readonly string Host;
        private readonly string Username;
        private readonly string Password;
        private readonly string Port;
        private readonly string _replyQueueName;
        private readonly EventingBasicConsumer consumer;
        private Response LastResponse;
        private readonly IBasicProperties props;

        private readonly IConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;


        public RabbitService(IOptions<Settings> config)
        {

            Host = config.Value.Rabbit.Host;
            Username = config.Value.Rabbit.Username;
            Password = config.Value.Rabbit.Password;
            Port = config.Value.Rabbit.Port;

            _factory = new ConnectionFactory()
            {
                HostName = Host,
                UserName = Username,
                Password = Password,
                Port = Convert.ToInt32(Port)
            };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

            _replyQueueName = _channel.QueueDeclare().QueueName;
            consumer = new EventingBasicConsumer(_channel);

            props = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = _replyQueueName;

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body.ToArray());
                var responseModel = JsonConvert.DeserializeObject<Response>(response);
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    Debug.WriteLine(responseModel.Message);
                    LastResponse = responseModel;
                }
            };

        }
        public void SubscribeQueue()
        {
            // User-1
        }
        public void SendMessageToUser(Message message)
        {
            var messageBytes = Encoding.UTF8
               .GetBytes(
                   JsonConvert.SerializeObject(message));

            _channel.BasicPublish(
                exchange: "users",
                routingKey: $"user.{message.Receiver}",
                basicProperties: props,
                body: messageBytes);

        }
        public void LogMessage(Message message)
        {
            var messageBytes = Encoding.UTF8
                .GetBytes(
                    JsonConvert.SerializeObject(message));

            _channel.BasicPublish(
                exchange: "logs",
                routingKey: "MsgLogs",
                basicProperties: props,
                body: messageBytes);

            _channel.BasicConsume(
                consumer: consumer,
                queue: _replyQueueName,
                autoAck: true);
        }

    }
}
