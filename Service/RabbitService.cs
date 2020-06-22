﻿using EdcsServer.Helper;
using EdcsServer.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

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
        private void QueueDeclare()
        {
            _channel.QueueDeclare(
               queue: QueueName,
               durable: false,
               exclusive: false,
               autoDelete: false,
               arguments: null);
            _channel.BasicQos(0, 1, false);
        }

        public void StartListening()
        {
            QueueDeclare();
            var consumer = new EventingBasicConsumer(_channel);
            _channel.BasicConsume(
                queue: QueueName,
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

                try
                {
                    var msg = _modelHelper.DeserializeJson<Message>(Encoding.UTF8.GetString(body));
                    if(_dbService.SaveMessage(msg))
                    {
                        // TODO: loggowanie wiadomości
                        Console.WriteLine("Zapisałem");
                    }
                    else
                    {
                        Console.WriteLine("Nie zapisałem");
                    }
                }
                catch(ArgumentNullException ex)
                {
                    // pusta wiadomość
                }
                catch (Exception ex)
                {

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
