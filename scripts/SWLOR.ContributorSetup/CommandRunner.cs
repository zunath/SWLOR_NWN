using System.Diagnostics;

namespace SWLOR.ContributorSetup;

internal class CommandRunner
{
    public virtual bool Exists(string name) => (Environment.GetEnvironmentVariable("PATH") ?? "")
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
        .Any(directory => File.Exists(Path.Combine(directory.Trim('"'), OperatingSystem.IsWindows() ? name + ".exe" : name)));

    public virtual async Task<CommandResult> RunAsync(string name, IEnumerable<string> arguments, string directory,
        IReadOnlyDictionary<string, string>? environment, bool capture, TimeSpan? timeout, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var start = new ProcessStartInfo(name)
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardOutput = capture,
            RedirectStandardError = capture
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        if (environment != null)
            foreach (var pair in environment) start.Environment[pair.Key] = pair.Value;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start " + name);
        var output = capture ? process.StandardOutput.ReadToEndAsync() : Task.FromResult("");
        var error = capture ? process.StandardError.ReadToEndAsync() : Task.FromResult("");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        if (timeout.HasValue) deadline.CancelAfter(timeout.Value);
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException)
        {
            // A foreground server also receives Ctrl+C. Give its container cleanup handler time to finish.
            if (!timeout.HasValue && cancellation.IsCancellationRequested)
            {
                using var grace = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try { await process.WaitForExitAsync(grace.Token); }
                catch (OperationCanceledException) { }
            }
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(output, error);
            cancellation.ThrowIfCancellationRequested();
            throw new TimeoutException(name + " timed out.");
        }
        return new(process.ExitCode, (await output).Trim(), (await error).Trim());
    }

    public async Task<string> CheckedAsync(string name, IEnumerable<string> arguments, string directory,
        IReadOnlyDictionary<string, string>? environment = null, bool capture = false, TimeSpan? timeout = null,
        CancellationToken cancellation = default)
    {
        var result = await RunAsync(name, arguments, directory, environment, capture,
            timeout ?? TimeSpan.FromMinutes(20), cancellation);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Command failed ({result.ExitCode}): {name} {result.Error} {result.Output}".Trim());
        return result.Output;
    }
}
