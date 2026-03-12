using Core.Events;

namespace Test.Core.Events;

public class VideoUploadedEventTests
{
    [Fact]
    public void InitProperties_ShouldStoreProvidedValues()
    {
        var videoId = Guid.NewGuid();
        var uploadedAt = DateTime.UtcNow;

        var evt = new VideoUploadedEvent
        {
            VideoId = videoId,
            UserId = 42,
            StoragePath = "videos/input.mp4",
            UploadedAt = uploadedAt
        };

        Assert.Equal(videoId, evt.VideoId);
        Assert.Equal(42, evt.UserId);
        Assert.Equal("videos/input.mp4", evt.StoragePath);
        Assert.Equal(uploadedAt, evt.UploadedAt);
    }
}
