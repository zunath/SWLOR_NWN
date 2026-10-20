using NUnit.Framework;

namespace SWLOR.ContributorSetup.Tests;

public class CommandRunnerTests
{
    [Test]
    public void PreCanceledCommandNeverStartsAChild()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await new CommandRunner().RunAsync("this-command-must-never-start", Array.Empty<string>(),
                Path.GetTempPath(), null, capture: true, timeout: null, cancellation.Token));
    }

    [Test]
    [CancelAfter(30000)]
    public void BoundedCommandIsTerminatedWhenItsDeadlineExpires()
    {
        var executable = OperatingSystem.IsWindows() ? Path.Combine(Environment.SystemDirectory, "ping.exe") : "/bin/sleep";
        var arguments = OperatingSystem.IsWindows() ? new[] { "127.0.0.1", "-t" } : new[] { "60" };
        Assert.ThrowsAsync<TimeoutException>(async () =>
            await new CommandRunner().RunAsync(executable, arguments, Path.GetTempPath(), null,
                capture: true, timeout: TimeSpan.FromMilliseconds(100), TestContext.CurrentContext.CancellationToken));
    }
}
