using Core.Interfaces;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Core.Helpers
{
    public class FfmpegFrameExtractor : IFrameExtractor
    {
        private const string WindowsFfmpegExecutablePath = @"C:\ffmpeg\bin\ffmpeg.exe";
        private readonly IFfmpegProcessRunner _processRunner;
        private readonly IFrameExtractionPathProvider _pathProvider;
        private readonly string _ffmpegExecutablePath;

        public FfmpegFrameExtractor()
            : this(new ProcessFfmpegRunner(), new DefaultFrameExtractionPathProvider(), ResolveFfmpegExecutablePath())
        {
        }

        public FfmpegFrameExtractor(IFfmpegProcessRunner processRunner, IFrameExtractionPathProvider pathProvider)
            : this(processRunner, pathProvider, ResolveFfmpegExecutablePath())
        {
        }

        public FfmpegFrameExtractor(IFfmpegProcessRunner processRunner, IFrameExtractionPathProvider pathProvider, string ffmpegExecutablePath)
        {
            _processRunner = processRunner;
            _pathProvider = pathProvider;
            _ffmpegExecutablePath = ffmpegExecutablePath;
        }

        public async Task<Stream> ExtractFramesAsync(Stream videoStream, Guid videoGuid, int fps)
        {
            var tempVideoPath = _pathProvider.GetTempVideoPath(videoGuid);
            var framesDir = _pathProvider.GetFramesDirectory(videoGuid);

            Directory.CreateDirectory(framesDir);

            using (var file = File.Create(tempVideoPath))
            {
                await videoStream.CopyToAsync(file);
            }

            var arguments = $"-i \"{tempVideoPath}\" -vf fps={fps} \"{framesDir}/frame_%04d.png\"";
            var result = await _processRunner.ExecuteAsync(_ffmpegExecutablePath, arguments);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"FFmpeg error: {result.ErrorOutput}");
            }

            var zipPath = _pathProvider.GetZipPath(videoGuid);

            ZipFile.CreateFromDirectory(framesDir, zipPath);

            return File.OpenRead(zipPath);
        }

        private static string ResolveFfmpegExecutablePath()
        {
            var configuredPath = Environment.GetEnvironmentVariable("FFMPEG_PATH");
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return configuredPath;
            }

            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? WindowsFfmpegExecutablePath
                : "ffmpeg";
        }
    }
}
