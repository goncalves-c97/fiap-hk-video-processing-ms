using Moq;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;

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

    [Fact]
    public async Task Subscribe_WhenMessageIsValid_ShouldInvokeHandlerAndAck()
    {
        var connection = new Mock<IConnection>(MockBehavior.Strict);
        var channel = new Mock<IModel>(MockBehavior.Strict);
        EventingBasicConsumer? consumer = null;
        var acked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        TestMessage? receivedMessage = null;

        connection.Setup(c => c.CreateModel()).Returns(channel.Object);
        channel.Setup(m => m.QueueDeclare("video-uploaded", true, false, false, null))
            .Returns(new QueueDeclareOk("video-uploaded", 0, 0));
        channel.Setup(m => m.BasicConsume("video-uploaded", false, string.Empty, false, false, null, It.IsAny<IBasicConsumer>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object>, IBasicConsumer>((_, _, _, _, _, _, basicConsumer) => consumer = Assert.IsType<EventingBasicConsumer>(basicConsumer))
            .Returns("consumer-tag");
        channel.Setup(m => m.BasicAck(7, false))
            .Callback(() => acked.TrySetResult());

        var bus = new RabbitMqEventBus(connection.Object);

        bus.Subscribe<TestMessage>("video-uploaded", message =>
        {
            receivedMessage = message;
            return Task.CompletedTask;
        });

        Assert.NotNull(consumer);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TestMessage { Name = "ok", Value = 99 }));
        consumer!.HandleBasicDeliver(
            consumerTag: "consumer-tag",
            deliveryTag: 7,
            redelivered: false,
            exchange: string.Empty,
            routingKey: "video-uploaded",
            properties: Mock.Of<IBasicProperties>(),
            body: body);

        await acked.Task;

        Assert.NotNull(receivedMessage);
        Assert.Equal("ok", receivedMessage!.Name);
        Assert.Equal(99, receivedMessage.Value);
        channel.Verify(m => m.BasicAck(7, false), Times.Once);
        channel.Verify(m => m.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Subscribe_WhenPayloadIsInvalid_ShouldNack()
    {
        var connection = new Mock<IConnection>(MockBehavior.Strict);
        var channel = new Mock<IModel>(MockBehavior.Strict);
        EventingBasicConsumer? consumer = null;
        var nacked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        connection.Setup(c => c.CreateModel()).Returns(channel.Object);
        channel.Setup(m => m.QueueDeclare("video-uploaded", true, false, false, null))
            .Returns(new QueueDeclareOk("video-uploaded", 0, 0));
        channel.Setup(m => m.BasicConsume("video-uploaded", false, string.Empty, false, false, null, It.IsAny<IBasicConsumer>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object>, IBasicConsumer>((_, _, _, _, _, _, basicConsumer) => consumer = Assert.IsType<EventingBasicConsumer>(basicConsumer))
            .Returns("consumer-tag");
        channel.Setup(m => m.BasicNack(11, false, true))
            .Callback(() => nacked.TrySetResult());

        var bus = new RabbitMqEventBus(connection.Object);

        bus.Subscribe<TestMessage>("video-uploaded", _ => Task.CompletedTask);

        Assert.NotNull(consumer);

        var invalidBody = Encoding.UTF8.GetBytes("{invalid-json");
        consumer!.HandleBasicDeliver(
            consumerTag: "consumer-tag",
            deliveryTag: 11,
            redelivered: false,
            exchange: string.Empty,
            routingKey: "video-uploaded",
            properties: Mock.Of<IBasicProperties>(),
            body: invalidBody);

        await nacked.Task;

        channel.Verify(m => m.BasicNack(11, false, true), Times.Once);
        channel.Verify(m => m.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Subscribe_WhenHandlerThrows_ShouldNack()
    {
        var connection = new Mock<IConnection>(MockBehavior.Strict);
        var channel = new Mock<IModel>(MockBehavior.Strict);
        EventingBasicConsumer? consumer = null;
        var nacked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        connection.Setup(c => c.CreateModel()).Returns(channel.Object);
        channel.Setup(m => m.QueueDeclare("video-uploaded", true, false, false, null))
            .Returns(new QueueDeclareOk("video-uploaded", 0, 0));
        channel.Setup(m => m.BasicConsume("video-uploaded", false, string.Empty, false, false, null, It.IsAny<IBasicConsumer>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object>, IBasicConsumer>((_, _, _, _, _, _, basicConsumer) => consumer = Assert.IsType<EventingBasicConsumer>(basicConsumer))
            .Returns("consumer-tag");
        channel.Setup(m => m.BasicNack(13, false, true))
            .Callback(() => nacked.TrySetResult());

        var bus = new RabbitMqEventBus(connection.Object);

        bus.Subscribe<TestMessage>("video-uploaded", _ => throw new InvalidOperationException("boom"));

        Assert.NotNull(consumer);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TestMessage { Name = "broken", Value = 1 }));
        consumer!.HandleBasicDeliver(
            consumerTag: "consumer-tag",
            deliveryTag: 13,
            redelivered: false,
            exchange: string.Empty,
            routingKey: "video-uploaded",
            properties: Mock.Of<IBasicProperties>(),
            body: body);

        await nacked.Task;

        channel.Verify(m => m.BasicNack(13, false, true), Times.Once);
        channel.Verify(m => m.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never);
    }

    private sealed class TestMessage
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
