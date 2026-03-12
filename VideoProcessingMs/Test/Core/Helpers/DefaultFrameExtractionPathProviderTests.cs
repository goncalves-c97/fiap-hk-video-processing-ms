using Core.Helpers;

namespace Test.Core.Helpers;

public class DefaultFrameExtractionPathProviderTests
{
    [Fact]
    public void GetPaths_ShouldUseTempDirectoryAndExpectedFileNames()
    {
        var videoGuid = Guid.NewGuid();
        var provider = new DefaultFrameExtractionPathProvider();
        var tempPath = Path.GetTempPath();

        var tempVideoPath = provider.GetTempVideoPath(videoGuid);
        var framesDirectory = provider.GetFramesDirectory(videoGuid);
        var zipPath = provider.GetZipPath(videoGuid);

        Assert.Equal(Path.Combine(tempPath, $"{videoGuid}.mp4"), tempVideoPath);
        Assert.Equal(Path.Combine(tempPath, videoGuid.ToString()), framesDirectory);
        Assert.Equal(Path.Combine(tempPath, $"{videoGuid}.zip"), zipPath);
    }
}
