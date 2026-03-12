using Core.Interfaces;
using System.Diagnostics;

namespace Core.Helpers
{
    public class ProcessFfmpegRunner : IFfmpegProcessRunner
    {
        public async Task<FfmpegProcessResult> ExecuteAsync(string executablePath, string arguments, CancellationToken cancellationToken = default)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();

            string error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            return new FfmpegProcessResult
            {
                ExitCode = process.ExitCode,
                ErrorOutput = error
            };
        }
    }
}
