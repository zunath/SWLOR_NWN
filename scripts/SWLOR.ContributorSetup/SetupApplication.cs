using System.Text.RegularExpressions;

namespace SWLOR.ContributorSetup;

internal sealed class SetupApplication(string root, CommandRunner runner, NativeTools native)
{
    internal async Task<int> RunAsync(SetupOptions options, CancellationToken cancellation)
    {
        var failures = new List<string>();
        foreach (var name in new[] { "git", "dotnet" })
        {
            if (!runner.Exists(name))
                failures.Add(name == "git" ? "Install Git." : "Install a stable .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0.");
            else Console.WriteLine("OK: " + name);
        }
        if (runner.Exists("dotnet"))
        {
            try
            {
                var version = await runner.CheckedAsync("dotnet", new[] { "--version" }, root,
                    capture: true, timeout: TimeSpan.FromSeconds(30), cancellation: cancellation);
                if (!Regex.IsMatch(version, @"^10\.\d+\.\d+$"))
                    failures.Add("Install a stable .NET 10 SDK. global.json excludes previews.");
                else Console.WriteLine("OK: .NET SDK " + version);
            }
            catch (Exception error) when (error is InvalidOperationException or TimeoutException)
            {
                failures.Add("Install a stable .NET 10 SDK. global.json selects the newest installed .NET 10 feature band.");
            }
        }
        failures.AddRange(native.CheckPrerequisites(options.Profile));
        if (options.Profile == "Server")
        {
            if (!runner.Exists("docker"))
                failures.Add("Install Docker Engine and the Compose plugin on Linux, or Docker Desktop on Windows.");
            else
            {
                try
                {
                    if (await runner.CheckedAsync("docker", new[] { "info", "--format", "{{.OSType}}" }, root,
                        capture: true, timeout: TimeSpan.FromSeconds(30), cancellation: cancellation) != "linux")
                        failures.Add("Configure Docker to run Linux containers.");
                    await runner.CheckedAsync("docker", new[] { "compose", "version" }, root,
                        capture: true, timeout: TimeSpan.FromSeconds(30), cancellation: cancellation);
                    Console.WriteLine("OK: Docker and Compose");
                }
                catch (Exception error) when (error is InvalidOperationException or TimeoutException)
                {
                    failures.Add("Start Docker and verify your account can access the daemon and Docker Compose.");
                }
            }
        }
        if (options.Mode == "Toolset" && OperatingSystem.IsLinux() && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
            failures.Add("StartToolset.sh needs X11 or Wayland with XWayland (DISPLAY).");
        if (options.Mode == "Check")
        {
            if (!File.Exists(Path.Combine(root, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json")))
                failures.Add("SWLOR assets are missing. Run Setup.cmd or ./Setup.sh.");
            if (!native.IsPrepared()) failures.Add("Native tools are missing or incomplete. Run Setup.cmd or ./Setup.sh.");
        }
        if (failures.Count != 0)
        {
            foreach (var failure in failures) Console.Error.WriteLine("NEEDS ATTENTION: " + failure);
            return 1;
        }
        if (options.Mode == "Check")
        {
            Console.WriteLine("Setup checks passed. No submodules, native tools, or containers were changed.");
            return 0;
        }

        await runner.CheckedAsync("git", new[] { "submodule", "update", "--init", "--recursive" }, root, cancellation: cancellation);
        var tools = await native.PrepareAsync(cancellation);
        var environment = native.EnvironmentFor(tools);
        var project = options.Profile == "Server" ? "SWLOR.Runner/SWLOR.Runner.csproj" : "SWLOR.Toolset/SWLOR.Toolset.csproj";
        await runner.CheckedAsync("dotnet", new[] { "build", project, "-p:RunPostBuildEvent=Never" }, root, environment, cancellation: cancellation);
        if (options.Mode == "Setup")
        {
            Console.WriteLine("Setup complete. Use StartToolset.cmd / ./StartToolset.sh or StartServer.cmd / ./StartServer.sh.");
            return 0;
        }
        if (options.Mode == "Server")
        {
            await runner.CheckedAsync("dotnet", new[] { "build", "SWLOR.CLI/SWLOR.CLI.csproj", "-c", "Release", "-p:RunPostBuildEvent=Never" },
                root, environment, cancellation: cancellation);
            var cli = Path.Combine(root, "SWLOR.CLI", "bin", "Release", "net10.0", "SWLOR.CLI.dll");
            await runner.CheckedAsync("dotnet", new[] { cli, "-p", "./Star Wars LOR v2.mod", "--no-prompt" },
                Path.Combine(root, "Module"), environment, cancellation: cancellation);
            await runner.CheckedAsync("dotnet", new[] { cli, "-o", "--server-output", Path.Combine(root, "SWLOR.Game.Server", "bin", "Debug", "net10.0") },
                Path.Combine(root, "Build"), environment, cancellation: cancellation);
        }
        var launch = await runner.RunAsync("dotnet", new[] { "run", "--no-build", "--project", project }, root,
            environment, capture: false, timeout: null, cancellation);
        return launch.ExitCode;
    }
}
