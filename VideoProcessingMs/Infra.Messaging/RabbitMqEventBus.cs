using Core.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Infra.Messaging
{
    public class RabbitMqEventBus : IMessagingService
    {
        private readonly IConnection _connection;

        public RabbitMqEventBus(IConnection connection)
        {
            _connection = connection;
        }

        public Task PublishAsync<T>(string routingKey, T message)
        {
            using var channel = _connection.CreateModel();

            channel.QueueDeclare(
                queue: routingKey,
                durable: true,
                exclusive: false,
                autoDelete: false);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            channel.BasicPublish(
                exchange: "",
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            return Task.CompletedTask;
        }

        public void Subscribe<T>(string queue, Func<T, Task> handler)
        {
            var channel = _connection.CreateModel();

            channel.QueueDeclare(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);

                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message != null)
                    {
                        await handler(message);
                    }

                    channel.BasicAck(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar mensagem: {ex.Message}");

                    channel.BasicNack(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: true);
                }
            };

            channel.BasicConsume(
                queue: queue,
                autoAck: false,
                consumer: consumer);
        }
    }
}