using Core.Helpers;
using System.Runtime.InteropServices;

namespace Test.Core.Helpers;

public class ProcessFfmpegRunnerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnExitCodeAndCapturedStandardError()
    {
        var runner = new ProcessFfmpegRunner();
        var (executablePath, arguments) = GetShellCommand();

        var result = await runner.ExecuteAsync(executablePath, arguments);

        Assert.Equal(5, result.ExitCode);
        Assert.Contains("runner-error", result.ErrorOutput);
    }

    private static (string ExecutablePath, string Arguments) GetShellCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return (Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe", "/c echo runner-error 1>&2 & exit 5");
        }

        return ("/bin/sh", "-c \"echo runner-error 1>&2; exit 5\"");
    }
}
