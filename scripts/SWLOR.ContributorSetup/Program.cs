using System.Runtime.InteropServices;

namespace SWLOR.ContributorSetup;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        try
        {
            var options = SetupOptions.Parse(args);
            if (options.Help)
            {
                Console.WriteLine("Usage: --mode Check|Setup|Toolset|Server [--profile Toolset|Server]");
                Console.WriteLine("  Check    List missing prerequisites. Leaves submodules, tools, and containers alone.");
                Console.WriteLine("  Setup    Fetch assets, prepare native tools, and build.");
                Console.WriteLine("  Toolset  Build and open the toolset.");
                Console.WriteLine("  Server   Pack the module and run a local server.");
                Console.WriteLine("  --profile selects what Check and Setup cover. Default: Toolset.");
                return 0;
            }
            if ((!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux()) ||
                RuntimeInformation.OSArchitecture != Architecture.X64 ||
                RuntimeInformation.ProcessArchitecture != Architecture.X64)
                throw new InvalidOperationException("Source setup currently supports x64 Windows and Linux.");

            var root = FindRepositoryRoot(Environment.CurrentDirectory);
            var runner = new CommandRunner();
            var native = new NativeTools(root, runner);
            return await new SetupApplication(root, runner, native).RunAsync(options, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Stopped.");
            return 130;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("Setup failed: " + error.Message);
            return 1;
        }
    }

    private static string FindRepositoryRoot(string start)
    {
        for (var directory = new DirectoryInfo(start); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "SWLOR.ContributorSetup", "SWLOR.ContributorSetup.csproj")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Run this command from the SWLOR source checkout.");
    }
}
