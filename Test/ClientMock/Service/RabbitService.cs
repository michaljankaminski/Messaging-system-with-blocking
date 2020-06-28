using EdcsClient.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows;

namespace EdcsClient.Service
{
    public interface IRabbitService
    {
        void LogMessage(Message message);
        void SendMessageToUser(Message message);
        Message GetLastMessage();
        void Listen();
    }
    public class RabbitService : IRabbitService
    {
        private readonly IOptions<Settings> _config;
        private readonly string Host;
        private readonly string Username;
        private readonly string Password;
        private readonly string Port;
        private readonly string _replyQueueName;
        private readonly EventingBasicConsumer consumer;
        private readonly EventingBasicConsumer userConsumer;
        private Response LastResponse;
        private readonly BlockingCollection<Message> respQueue = new BlockingCollection<Message>();
        private readonly IBasicProperties props;

        private readonly IConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;


        public RabbitService(IOptions<Settings> config)
        {
            _config = config;
            Host = _config.Value.Rabbit.Host;
            Username = _config.Value.Rabbit.Username;
            Password = _config.Value.Rabbit.Password;
            Port = _config.Value.Rabbit.Port;

            _factory = new ConnectionFactory()
            {
                HostName = Host,
                UserName = Username,
                Password = Password,
                Port = Convert.ToInt32(Port),
                AutomaticRecoveryEnabled = true
            };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

            _replyQueueName = _channel.QueueDeclare().QueueName;
            consumer = new EventingBasicConsumer(_channel);
            userConsumer = new EventingBasicConsumer(_channel);

            props = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = _replyQueueName;

            userConsumer.Received += ReceiveMessage;
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
        private void ReceiveMessage(object model, BasicDeliverEventArgs ea)
        {
            Debug.WriteLine("Dostałem wiadomość na kolejce");

            var body = ea.Body.ToArray();
            var response = Encoding.UTF8.GetString(body.ToArray());
            var responseModel = JsonConvert.DeserializeObject<Message>(response);
            respQueue.Add(responseModel);
        }

        public void SendMessageToUser(Message message)
        {
            var messageBytes = Encoding.UTF8
               .GetBytes(
                   JsonConvert.SerializeObject(message));

            _channel.BasicPublish(
                exchange: "users",
                routingKey: $"user.{message.Receiver}",
                basicProperties: null,
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
        public void Listen()
        {
            try
            {
                if(MainWindow.CurrentUser != null)
                {
                    _channel.BasicConsume(
                    queue: $"user-{MainWindow.CurrentUser.Id}",
                    autoAck: true,
                    consumer: userConsumer
                    );
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show("I am not able to connect to a server now. Please try later!", 
                    "Connection error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
        public Message GetLastMessage()
        {
            if (respQueue.Count > 0)
                return respQueue.Take();
            else
                return null;
        }

    }
}
