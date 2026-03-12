using Infra.Messaging;
using Moq;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Test.Infra.Messaging;

public class RabbitMqEventBusTests
{
    [Fact]
    public async Task PublishAsync_DeclaresQueueAsDurableAndPublishesPersistentMessage()
    {
        var connection = new Mock<IConnection>(MockBehavior.Strict);
        var channel = new Mock<IModel>(MockBehavior.Strict);
        var props = new Mock<IBasicProperties>();

        // Create model
        connection.Setup(c => c.CreateModel()).Returns(channel.Object);

        // Declare queue
        channel.Setup(m => m.QueueDeclare(
        "my-queue",
        true,
        false,
        false,
        null
       )).Returns(new QueueDeclareOk("my-queue",0,0));

        // Properties
        channel.Setup(m => m.CreateBasicProperties()).Returns(props.Object);
        props.SetupSet(p => p.Persistent = true);

        // Publish capture
        string? exchange = null;
        string? routingKey = null;
        IBasicProperties? basicProperties = null;
        ReadOnlyMemory<byte> body = default;

        channel.Setup(m => m.BasicPublish(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<bool>(),
        It.IsAny<IBasicProperties>(),
        It.IsAny<ReadOnlyMemory<byte>>()
        )).Callback((string ex, string rk, bool _, IBasicProperties bp, ReadOnlyMemory<byte> b) =>
        {
            exchange = ex;
            routingKey = rk;
            basicProperties = bp;
            body = b;
        });

        // Dispose
        channel.Setup(m => m.Dispose());

        var bus = new RabbitMqEventBus(connection.Object);

        var message = new { Name = "test", Value = 123 };
        await bus.PublishAsync("my-queue", message);

        // verify declare parameters
        channel.Verify(m => m.QueueDeclare(
        "my-queue",
        true,
        false,
        false,
        null
        ), Times.Once);

        Assert.Equal("", exchange);
        Assert.Equal("my-queue", routingKey);
        Assert.Same(props.Object, basicProperties);

        var json = Encoding.UTF8.GetString(body.ToArray());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("test", doc.RootElement.GetProperty("Name").GetString());
        Assert.Equal(123, doc.RootElement.GetProperty("Value").GetInt32());
    }
}
