using Core.Interfaces;
using System.IO.Compression;

namespace Core.Helpers
{
    public class FfmpegFrameExtractor : IFrameExtractor
    {
        private const string FfmpegExecutablePath = @"C:\ffmpeg\bin\ffmpeg.exe";
        private readonly IFfmpegProcessRunner _processRunner;
        private readonly IFrameExtractionPathProvider _pathProvider;

        public FfmpegFrameExtractor()
            : this(new ProcessFfmpegRunner(), new DefaultFrameExtractionPathProvider())
        {
        }

        public FfmpegFrameExtractor(IFfmpegProcessRunner processRunner, IFrameExtractionPathProvider pathProvider)
        {
            _processRunner = processRunner;
            _pathProvider = pathProvider;
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
            var result = await _processRunner.ExecuteAsync(FfmpegExecutablePath, arguments);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"FFmpeg error: {result.ErrorOutput}");
            }

            var zipPath = _pathProvider.GetZipPath(videoGuid);

            ZipFile.CreateFromDirectory(framesDir, zipPath);

            return File.OpenRead(zipPath);
        }
    }
}
