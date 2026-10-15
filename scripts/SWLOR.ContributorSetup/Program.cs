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
                Console.WriteLine("Usage: --mode Check|Setup|Toolset|Server --profile Toolset|Server");
                Console.WriteLine("Check diagnoses prerequisites; Setup prepares and builds; Toolset/Server build and launch.");
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
