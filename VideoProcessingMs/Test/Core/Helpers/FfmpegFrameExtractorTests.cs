using Core.Helpers;
using Core.Interfaces;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Test.Core.Helpers;

public class FfmpegFrameExtractorTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"ffmpeg-extractor-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ExtractFramesAsync_WhenProcessSucceeds_ShouldCreateZipFromGeneratedFrames()
    {
        Directory.CreateDirectory(_tempRoot);
        var videoGuid = Guid.NewGuid();
        var pathProvider = new TestFrameExtractionPathProvider(_tempRoot);
        var runner = new FakeFfmpegProcessRunner((executablePath, arguments) =>
        {
            Assert.Equal("custom-ffmpeg", executablePath);
            Assert.Contains($"-vf fps=2", arguments);
            Assert.Contains(pathProvider.GetTempVideoPath(videoGuid), arguments);

            var framesDirectory = pathProvider.GetFramesDirectory(videoGuid);
            Directory.CreateDirectory(framesDirectory);
            File.WriteAllText(Path.Combine(framesDirectory, "frame_0001.png"), "frame-content");

            return new FfmpegProcessResult
            {
                ExitCode = 0
            };
        });

        var extractor = new FfmpegFrameExtractor(runner, pathProvider, "custom-ffmpeg");
        await using var sourceVideo = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        await using var zipStream = await extractor.ExtractFramesAsync(sourceVideo, videoGuid, 2);

        Assert.True(File.Exists(pathProvider.GetTempVideoPath(videoGuid)));
        Assert.True(File.Exists(pathProvider.GetZipPath(videoGuid)));

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal("frame_0001.png", entry.FullName);
    }

    [Fact]
    public async Task ExtractFramesAsync_WhenProcessFails_ShouldThrowAndSkipZipCreation()
    {
        Directory.CreateDirectory(_tempRoot);
        var videoGuid = Guid.NewGuid();
        var pathProvider = new TestFrameExtractionPathProvider(_tempRoot);
        var runner = new FakeFfmpegProcessRunner((_, _) => new FfmpegProcessResult
        {
            ExitCode = 1,
            ErrorOutput = "simulated failure"
        });

        var extractor = new FfmpegFrameExtractor(runner, pathProvider, "custom-ffmpeg");
        await using var sourceVideo = new MemoryStream(new byte[] { 9, 8, 7 });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            extractor.ExtractFramesAsync(sourceVideo, videoGuid, 3));

        Assert.Equal("FFmpeg error: simulated failure", exception.Message);
        Assert.True(File.Exists(pathProvider.GetTempVideoPath(videoGuid)));
        Assert.False(File.Exists(pathProvider.GetZipPath(videoGuid)));
    }

    [Fact]
    public async Task ExtractFramesAsync_WhenEnvironmentVariableIsSet_ShouldUseConfiguredFfmpegPath()
    {
        Directory.CreateDirectory(_tempRoot);
        var previousValue = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        Environment.SetEnvironmentVariable("FFMPEG_PATH", "env-ffmpeg");

        try
        {
            var videoGuid = Guid.NewGuid();
            var pathProvider = new TestFrameExtractionPathProvider(_tempRoot);
            var runner = new FakeFfmpegProcessRunner((executablePath, _) =>
            {
                Assert.Equal("env-ffmpeg", executablePath);

                var framesDirectory = pathProvider.GetFramesDirectory(videoGuid);
                Directory.CreateDirectory(framesDirectory);
                File.WriteAllText(Path.Combine(framesDirectory, "frame_0001.png"), "frame-content");

                return new FfmpegProcessResult { ExitCode = 0 };
            });

            var extractor = new FfmpegFrameExtractor(runner, pathProvider);
            await using var sourceVideo = new MemoryStream(new byte[] { 1, 2, 3 });

            await using var zipStream = await extractor.ExtractFramesAsync(sourceVideo, videoGuid, 1);

            Assert.NotNull(zipStream);
        }
        finally
        {
            Environment.SetEnvironmentVariable("FFMPEG_PATH", previousValue);
        }
    }

    [Fact]
    public async Task ExtractFramesAsync_WhenEnvironmentVariableIsMissing_ShouldUsePlatformDefaultPath()
    {
        Directory.CreateDirectory(_tempRoot);
        var previousValue = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        Environment.SetEnvironmentVariable("FFMPEG_PATH", null);

        try
        {
            var videoGuid = Guid.NewGuid();
            var pathProvider = new TestFrameExtractionPathProvider(_tempRoot);
            var expectedPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? @"C:\ffmpeg\bin\ffmpeg.exe"
                : "ffmpeg";

            var runner = new FakeFfmpegProcessRunner((executablePath, _) =>
            {
                Assert.Equal(expectedPath, executablePath);

                var framesDirectory = pathProvider.GetFramesDirectory(videoGuid);
                Directory.CreateDirectory(framesDirectory);
                File.WriteAllText(Path.Combine(framesDirectory, "frame_0001.png"), "frame-content");

                return new FfmpegProcessResult { ExitCode = 0 };
            });

            var extractor = new FfmpegFrameExtractor(runner, pathProvider);
            await using var sourceVideo = new MemoryStream(new byte[] { 1, 2, 3 });

            await using var zipStream = await extractor.ExtractFramesAsync(sourceVideo, videoGuid, 1);

            Assert.NotNull(zipStream);
        }
        finally
        {
            Environment.SetEnvironmentVariable("FFMPEG_PATH", previousValue);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, true);
        }
    }

    private sealed class FakeFfmpegProcessRunner : IFfmpegProcessRunner
    {
        private readonly Func<string, string, FfmpegProcessResult> _handler;

        public FakeFfmpegProcessRunner(Func<string, string, FfmpegProcessResult> handler)
        {
            _handler = handler;
        }

        public Task<FfmpegProcessResult> ExecuteAsync(string executablePath, string arguments, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_handler(executablePath, arguments));
        }
    }

    private sealed class TestFrameExtractionPathProvider : IFrameExtractionPathProvider
    {
        private readonly string _root;

        public TestFrameExtractionPathProvider(string root)
        {
            _root = root;
        }

        public string GetTempVideoPath(Guid videoGuid)
        {
            return Path.Combine(_root, $"{videoGuid}.mp4");
        }

        public string GetFramesDirectory(Guid videoGuid)
        {
            return Path.Combine(_root, videoGuid.ToString());
        }

        public string GetZipPath(Guid videoGuid)
        {
            return Path.Combine(_root, $"{videoGuid}.zip");
        }
    }
}
