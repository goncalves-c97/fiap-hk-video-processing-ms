using Core.Helpers;

namespace Test.Core.Helpers;

public class ProcessFfmpegRunnerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnExitCodeAndCapturedStandardError()
    {
        var runner = new ProcessFfmpegRunner();
        var executablePath = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
        var arguments = "/c echo runner-error 1>&2 & exit 5";

        var result = await runner.ExecuteAsync(executablePath, arguments);

        Assert.Equal(5, result.ExitCode);
        Assert.Contains("runner-error", result.ErrorOutput);
    }
}
