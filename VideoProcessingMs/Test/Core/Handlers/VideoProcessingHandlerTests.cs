using Core.Entities;
using Core.Enums;
using Core.Events;
using Core.Exceptions;
using Core.Handlers;
using Core.Interfaces;
using Core.Interfaces.Gateways;
using Moq;

namespace Test.Core.Handlers;

public class VideoProcessingHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenVideoExists_ShouldProcessAndMarkAsCompleted()
    {
        var evt = CreateEvent();
        var video = CreateVideoUpload(evt);
        var expectedZipPath = $"{video.Guid}-processed/{video.Guid}.zip";
        var expectedFullZipPath = $"videos/{expectedZipPath}";
        var expectedZipUrl = $"https://storage.local/{video.Guid}.zip";
        var statusSnapshots = new List<(StatusVideoEnum Status, DateTime? StartedAt, DateTime? FinishedAt, string? ZipPath, string? ErrorMessage, int Attempts)>();
        var gateway = new Mock<IVideoProcessingGateway>(MockBehavior.Strict);
        var storage = new Mock<IObjectStorageService>(MockBehavior.Strict);
        var frameExtractor = new Mock<IFrameExtractor>(MockBehavior.Strict);
        var inputStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var zipStream = new MemoryStream(new byte[] { 4, 5, 6 });
        var handler = new VideoProcessingHandler();

        gateway.Setup(x => x.GetByGuid(evt.VideoId, evt.UserId)).ReturnsAsync(video);
        gateway.Setup(x => x.Update(It.IsAny<VideoUpload>()))
            .Callback<VideoUpload>(v => statusSnapshots.Add((v.Status, v.DataHoraInicioProcessamento, v.DataHoraFimProcessamento, v.CaminhoZipProcessado, v.MensagemErro, v.TentativasProcessamento)))
            .Returns(Task.CompletedTask);
        storage.Setup(x => x.DownloadAsync(evt.StoragePath)).ReturnsAsync(inputStream);
        frameExtractor.Setup(x => x.ExtractFramesAsync(inputStream, video.Guid, 2)).ReturnsAsync(zipStream);
        storage.Setup(x => x.UploadAsync(zipStream, expectedZipPath, "application/zip"))
            .ReturnsAsync(expectedFullZipPath);
        storage.Setup(x => x.GetPresignedUrl(expectedFullZipPath, 60)).Returns(expectedZipUrl);

        var zipUrl = await handler.HandleAsync(evt, gateway.Object, storage.Object, frameExtractor.Object);

        Assert.Equal(expectedZipUrl, zipUrl);
        Assert.Equal(StatusVideoEnum.Completed, video.Status);
        Assert.Equal(expectedZipUrl, video.CaminhoZipProcessado);
        Assert.NotNull(video.DataHoraInicioProcessamento);
        Assert.NotNull(video.DataHoraFimProcessamento);
        Assert.Null(video.MensagemErro);
        Assert.Equal(0, video.TentativasProcessamento);
        Assert.Collection(statusSnapshots,
            first =>
            {
                Assert.Equal(StatusVideoEnum.Processing, first.Status);
                Assert.NotNull(first.StartedAt);
                Assert.Null(first.FinishedAt);
                Assert.Null(first.ZipPath);
            },
            second =>
            {
                Assert.Equal(StatusVideoEnum.Completed, second.Status);
                Assert.NotNull(second.StartedAt);
                Assert.NotNull(second.FinishedAt);
                Assert.Equal(expectedZipUrl, second.ZipPath);
            });

        gateway.Verify(x => x.GetByGuid(evt.VideoId, evt.UserId), Times.Once);
        gateway.Verify(x => x.Update(It.IsAny<VideoUpload>()), Times.Exactly(2));
        storage.Verify(x => x.DownloadAsync(evt.StoragePath), Times.Once);
        frameExtractor.Verify(x => x.ExtractFramesAsync(inputStream, video.Guid, 2), Times.Once);
        storage.Verify(x => x.UploadAsync(zipStream, expectedZipPath, "application/zip"), Times.Once);
        storage.Verify(x => x.GetPresignedUrl(expectedFullZipPath, 60), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenVideoIsMissing_ShouldThrowWithoutUpdating()
    {
        var evt = CreateEvent();
        var gateway = new Mock<IVideoProcessingGateway>(MockBehavior.Strict);
        var storage = new Mock<IObjectStorageService>(MockBehavior.Strict);
        var frameExtractor = new Mock<IFrameExtractor>(MockBehavior.Strict);
        var handler = new VideoProcessingHandler();

        gateway.Setup(x => x.GetByGuid(evt.VideoId, evt.UserId)).ReturnsAsync((VideoUpload?)null);

        var exception = await Assert.ThrowsAsync<VideoNotFoundException>(() =>
            handler.HandleAsync(evt, gateway.Object, storage.Object, frameExtractor.Object));

        Assert.Equal($"Vídeo {evt.VideoId} não encontrado.", exception.Message);
        gateway.Verify(x => x.Update(It.IsAny<VideoUpload>()), Times.Never);
        storage.VerifyNoOtherCalls();
        frameExtractor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenProcessingFailsAfterLoad_ShouldMarkVideoAsErrorAndIncrementAttempts()
    {
        var evt = CreateEvent();
        var video = CreateVideoUpload(evt);
        var statusSnapshots = new List<(StatusVideoEnum Status, DateTime? StartedAt, DateTime? FinishedAt, string? ZipPath, string? ErrorMessage, int Attempts)>();
        var gateway = new Mock<IVideoProcessingGateway>(MockBehavior.Strict);
        var storage = new Mock<IObjectStorageService>(MockBehavior.Strict);
        var frameExtractor = new Mock<IFrameExtractor>(MockBehavior.Strict);
        var inputStream = new MemoryStream(new byte[] { 7, 8, 9 });
        var handler = new VideoProcessingHandler();

        gateway.Setup(x => x.GetByGuid(evt.VideoId, evt.UserId)).ReturnsAsync(video);
        gateway.Setup(x => x.Update(It.IsAny<VideoUpload>()))
            .Callback<VideoUpload>(v => statusSnapshots.Add((v.Status, v.DataHoraInicioProcessamento, v.DataHoraFimProcessamento, v.CaminhoZipProcessado, v.MensagemErro, v.TentativasProcessamento)))
            .Returns(Task.CompletedTask);
        storage.Setup(x => x.DownloadAsync(evt.StoragePath)).ReturnsAsync(inputStream);
        frameExtractor.Setup(x => x.ExtractFramesAsync(inputStream, video.Guid, 2))
            .ThrowsAsync(new InvalidOperationException("zip failure"));

        var exception = await Assert.ThrowsAsync<VideoProcessingException>(() =>
            handler.HandleAsync(evt, gateway.Object, storage.Object, frameExtractor.Object));

        Assert.Equal("zip failure", exception.Message);
        Assert.Equal(1, exception.Attempts);
        Assert.Equal(StatusVideoEnum.Error, video.Status);
        Assert.Equal("zip failure", video.MensagemErro);
        Assert.Equal(1, video.TentativasProcessamento);
        Assert.NotNull(video.DataHoraInicioProcessamento);
        Assert.Collection(statusSnapshots,
            first =>
            {
                Assert.Equal(StatusVideoEnum.Processing, first.Status);
                Assert.NotNull(first.StartedAt);
                Assert.Null(first.FinishedAt);
                Assert.Null(first.ErrorMessage);
                Assert.Equal(0, first.Attempts);
            },
            second =>
            {
                Assert.Equal(StatusVideoEnum.Error, second.Status);
                Assert.Equal("zip failure", second.ErrorMessage);
                Assert.Equal(1, second.Attempts);
            });

        gateway.Verify(x => x.Update(It.IsAny<VideoUpload>()), Times.Exactly(2));
        storage.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenAttemptsReachLimit_ShouldMarkVideoAsAttemptsExceeded()
    {
        var evt = CreateEvent();
        var video = CreateVideoUpload(evt);
        video.TentativasProcessamento = 9;

        var gateway = new Mock<IVideoProcessingGateway>(MockBehavior.Strict);
        var storage = new Mock<IObjectStorageService>(MockBehavior.Strict);
        var frameExtractor = new Mock<IFrameExtractor>(MockBehavior.Strict);
        var inputStream = new MemoryStream(new byte[] { 7, 8, 9 });
        var handler = new VideoProcessingHandler();

        gateway.Setup(x => x.GetByGuid(evt.VideoId, evt.UserId)).ReturnsAsync(video);
        gateway.Setup(x => x.Update(It.IsAny<VideoUpload>())).Returns(Task.CompletedTask);
        storage.Setup(x => x.DownloadAsync(evt.StoragePath)).ReturnsAsync(inputStream);
        frameExtractor.Setup(x => x.ExtractFramesAsync(inputStream, video.Guid, 2))
            .ThrowsAsync(new InvalidOperationException("zip failure"));

        var exception = await Assert.ThrowsAsync<VideoProcessingException>(() =>
            handler.HandleAsync(evt, gateway.Object, storage.Object, frameExtractor.Object));

        Assert.Equal("zip failure", exception.Message);
        Assert.Equal(10, exception.Attempts);
        Assert.Equal(StatusVideoEnum.ErrorAttemptsExceeded, video.Status);
        Assert.Equal(10, video.TentativasProcessamento);
        gateway.Verify(x => x.Update(It.IsAny<VideoUpload>()), Times.Exactly(2));
    }

    private static VideoUploadedEvent CreateEvent()
    {
        return new VideoUploadedEvent
        {
            VideoId = Guid.NewGuid(),
            UserId = 12,
            UserEmail = "user@example.com",
            OriginalVideoName = "raw-video.mp4",
            StoragePath = "uploads/raw-video.mp4",
            UploadedAt = DateTime.UtcNow
        };
    }

    private static VideoUpload CreateVideoUpload(VideoUploadedEvent evt)
    {
        return new VideoUpload
        {
            IdVideo = 1,
            Guid = evt.VideoId,
            IdUsuario = evt.UserId,
            NomeArquivoOriginal = "raw-video.mp4",
            CaminhoStorageOriginal = evt.StoragePath,
            Status = StatusVideoEnum.Pending,
            DataHoraUpload = evt.UploadedAt,
            TentativasProcessamento = 0
        };
    }
}
