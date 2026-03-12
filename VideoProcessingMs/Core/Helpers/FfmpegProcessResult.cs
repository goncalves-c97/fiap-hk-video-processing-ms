namespace Core.Helpers
{
    public sealed class FfmpegProcessResult
    {
        public int ExitCode { get; init; }
        public string ErrorOutput { get; init; } = string.Empty;
    }
}
