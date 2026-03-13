using Core.Events;
using Core.Interfaces;
using Core.Interfaces.Gateways;
using Moq;

namespace Test.VideoProcessorWorker;

public class VideoProcessingWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldSubscribeToQueueAndDelegateEventsToHandler()
    {
        var consumer = new Mock<IMessagingService>(MockBehavior.Strict);
        var handler = new Mock<IVideoProcessingHandler>(MockBehavior.Strict);
        var gateway = new Mock<IVideoProcessingGateway>(MockBehavior.Strict);
        var storage = new Mock<IObjectStorageService>(MockBehavior.Strict);
        var frameExtractor = new Mock<IFrameExtractor>(MockBehavior.Strict);
        var evt = new VideoUploadedEvent
        {
            VideoId = Guid.NewGuid(),
            UserId = 77,
            UserEmail = "worker@example.com",
            OriginalVideoName = "video.mp4",
            StoragePath = "video.mp4",
            UploadedAt = DateTime.UtcNow
        };

        Func<VideoUploadedEvent, Task>? subscriptionHandler = null;
        consumer.Setup(x => x.Subscribe<VideoUploadedEvent>("video-uploaded", It.IsAny<Func<VideoUploadedEvent, Task>>()))
            .Callback<string, Func<VideoUploadedEvent, Task>>((_, callback) => subscriptionHandler = callback);

        handler.Setup(x => x.HandleAsync(evt, gateway.Object, storage.Object, frameExtractor.Object, default))
            .ReturnsAsync("https://storage.local/processed.zip");

        consumer.Setup(x => x.PublishAsync("video-processed", It.Is<VideoProcessedEvent>(message =>
            message.VideoId == evt.VideoId &&
            message.UserId == evt.UserId &&
            message.UserEmail == evt.UserEmail &&
            message.OriginalVideoName == evt.OriginalVideoName &&
            message.ProcessedVideoUrl == "https://storage.local/processed.zip")))
            .Returns(Task.CompletedTask);

        var worker = new TestableVideoProcessingWorker(
            consumer.Object,
            handler.Object,
            gateway.Object,
            storage.Object,
            frameExtractor.Object);

        using var cancellation = new CancellationTokenSource();
        var executionTask = worker.RunExecuteAsync(cancellation.Token);

        Assert.NotNull(subscriptionHandler);

        await subscriptionHandler!(evt);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executionTask);

        consumer.Verify(x => x.Subscribe<VideoUploadedEvent>("video-uploaded", It.IsAny<Func<VideoUploadedEvent, Task>>()), Times.Once);
        handler.Verify(x => x.HandleAsync(evt, gateway.Object, storage.Object, frameExtractor.Object, default), Times.Once);
        consumer.Verify(x => x.PublishAsync("video-processed", It.IsAny<VideoProcessedEvent>()), Times.Once);
    }

    private sealed class TestableVideoProcessingWorker : global::VideoProcessorWorker.VideoProcessingWorker
    {
        public TestableVideoProcessingWorker(
            IMessagingService consumer,
            IVideoProcessingHandler handler,
            IVideoProcessingGateway videoProcessingGateway,
            IObjectStorageService objectStorageService,
            IFrameExtractor frameExtractor)
            : base(consumer, handler, videoProcessingGateway, objectStorageService, frameExtractor)
        {
        }

        public Task RunExecuteAsync(CancellationToken cancellationToken)
        {
            return ExecuteAsync(cancellationToken);
        }
    }
}
