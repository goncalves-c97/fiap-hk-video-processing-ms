using Core.Helpers;

namespace Core.Interfaces
{
    public interface IFfmpegProcessRunner
    {
        Task<FfmpegProcessResult> ExecuteAsync(string executablePath, string arguments, CancellationToken cancellationToken = default);
    }
}
