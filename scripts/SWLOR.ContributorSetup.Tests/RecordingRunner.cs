namespace SWLOR.ContributorSetup.Tests;

internal sealed class RecordingRunner : CommandRunner
{
    internal List<(string Name, string[] Arguments, string Directory, IReadOnlyDictionary<string, string>? Environment, TimeSpan? Timeout)> Commands { get; } = new();
    internal string Version { get; set; } = "10.0.402";
    internal string? MissingCommand { get; set; }
    internal bool FailBuild { get; set; }
    internal int LaunchExitCode { get; set; }

    public override bool Exists(string name) => name != MissingCommand;
    public override Task<CommandResult> RunAsync(string name, IEnumerable<string> arguments, string directory,
        IReadOnlyDictionary<string, string>? environment, bool capture, TimeSpan? timeout, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var args = arguments.ToArray();
        Commands.Add((name, args, directory, environment, timeout));
        if (args[0] == "--version") return Task.FromResult(new CommandResult(0, Version));
        if (name == "docker") return Task.FromResult(new CommandResult(0, args[0] == "info" ? "linux" : "Compose"));
        if (FailBuild && args[0] == "build") return Task.FromResult(new CommandResult(1, Error: "Build failed"));
        return Task.FromResult(new CommandResult(args[0] == "run" ? LaunchExitCode : 0));
    }
}
