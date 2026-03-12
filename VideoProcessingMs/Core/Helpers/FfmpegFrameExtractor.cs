using Core.Interfaces;
using System.Diagnostics;
using System.IO.Compression;

namespace Core.Helpers
{
    public class FfmpegFrameExtractor : IFrameExtractor
    {
        public async Task<Stream> ExtractFramesAsync(Stream videoStream, Guid videoGuid, int framesPerSecond)
        {
            var tempVideoPath = Path.Combine(Path.GetTempPath(), $"{videoGuid}.mp4");
            var framesDir = Path.Combine(Path.GetTempPath(), videoGuid.ToString());

            Directory.CreateDirectory(framesDir);

            using (var file = File.Create(tempVideoPath))
            {
                await videoStream.CopyToAsync(file);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"C:\ffmpeg\bin\ffmpeg.exe", // ffmpeg path TODO: When deploying, ensure ffmpeg is included in the deployment package and update this path accordingly
                    Arguments = $"-i \"{tempVideoPath}\" -vf fps={framesPerSecond} \"{framesDir}/frame_%04d.png\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();

            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg error: {error}");
            }

            var zipPath = Path.Combine(Path.GetTempPath(), $"{videoGuid}.zip");

            ZipFile.CreateFromDirectory(framesDir, zipPath);

            return File.OpenRead(zipPath);
        }
    }
}
