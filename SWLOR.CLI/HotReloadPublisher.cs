using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Nwn.Hosting.Contracts.Generations;
using SWLOR.Game.Host.Reload;

namespace SWLOR.CLI
{
    /// <summary>
    /// <c>--reload</c>: replaces the game build of a running development or test server without restarting it. Builds
    /// SWLOR.Game.Server in Release (or takes <c>--server-output</c>), publishes the output as a new candidate into the
    /// server home's <c>dotnet/generations/&lt;id&gt;/</c>, writes the reload signal naming it to <c>reload/signal.json</c>, then
    /// waits for the server's receipt (<c>reload/receipts/&lt;id&gt;.json</c>) and prints it. The server must boot the host
    /// entry with <c>SWLOR_HOT_RELOAD=true</c> (<c>SWLOR.Game.Host</c>'s <c>ReloadTrigger</c>). See
    /// <c>Readmes/README_HotReload.md</c>.
    /// </summary>
    internal sealed class HotReloadPublisher
    {
        /// <summary>How long the tool waits for the final receipt by default; a reload's pause includes the new build's first JIT.</summary>
        public const int DefaultTimeoutSeconds = 600;

        /// <summary>How many candidates stay in <c>dotnet/generations/</c> after an applied reload, the running one included.</summary>
        public const int CandidatesKept = 3;

        /// <summary>The host assembly a server home that can reload has in <c>dotnet/</c>.</summary>
        public const string HostAssemblyFileName = "SWLOR.Game.Host.dll";

        private static readonly TimeSpan ReceiptPollInterval = TimeSpan.FromMilliseconds(250);

        private readonly TextWriter _out;
        private readonly Func<string, int> _build;
        private readonly TimeSpan _pollInterval;

        public HotReloadPublisher()
            : this(Console.Out, BuildRelease, ReceiptPollInterval)
        {
        }

        /// <summary>For tests: the console, the build of a project file and the receipt poll interval are replaced.</summary>
        internal HotReloadPublisher(TextWriter output, Func<string, int> build, TimeSpan pollInterval)
        {
            _out = output ?? throw new ArgumentNullException(nameof(output));
            _build = build ?? throw new ArgumentNullException(nameof(build));
            _pollInterval = pollInterval;
        }

        /// <summary>
        /// Builds (unless <paramref name="serverOutputPath"/> names a build output), publishes, signals and waits. Returns 0
        /// when the server applied the candidate, 1 otherwise.
        /// </summary>
        /// <param name="serverOutputPath">A SWLOR.Game.Server build output to publish as is; null builds the Release output.</param>
        /// <param name="serverHomePath">The server home (<c>dotnet/</c>, <c>reload/</c>); null is the repository's <c>debugserver/</c>.</param>
        /// <param name="timeoutSeconds">How long to wait for the final receipt; 0 or less is <see cref="DefaultTimeoutSeconds"/>.</param>
        /// <param name="workingDirectory">Where the repository is looked for; the current directory when null.</param>
        public int Process(string serverOutputPath, string serverHomePath, int timeoutSeconds, string workingDirectory = null)
        {
            var repositoryRoot = FindRepositoryRoot(workingDirectory ?? Directory.GetCurrentDirectory());
            var serverHome = !string.IsNullOrWhiteSpace(serverHomePath)
                ? Path.GetFullPath(serverHomePath)
                : repositoryRoot == null ? null : Path.Combine(repositoryRoot, "debugserver");
            if (serverHome == null)
                return Fail("No server home: pass --server-home, or run from the repository to use its debugserver/.");

            var layout = ReloadLayout.ForServerHome(serverHome);
            if (!File.Exists(Path.Combine(layout.GenerationRoot, HostAssemblyFileName)))
                return Fail($"{layout.GenerationRoot} has no {HostAssemblyFileName}: hot reload needs a server that boots the host entry " +
                            "(SWLOR_DOTNET_ENTRY=host). Deploy it first (RunCLI.cmd -o).");

            string output;
            if (string.IsNullOrWhiteSpace(serverOutputPath))
            {
                if (repositoryRoot == null)
                    return Fail("Not in the repository: run from it to build the game, or pass --server-output with a build to publish.");

                var project = Path.Combine(repositoryRoot, "SWLOR.Game.Server", "SWLOR.Game.Server.csproj");
                _out.WriteLine($"Building {project} (Release)...");
                if (_build(project) != 0)
                    return Fail("The build failed; nothing was published and the server keeps its build.");
                output = Path.Combine(repositoryRoot, "SWLOR.Game.Server", "bin", "Release", "net10.0");
            }
            else
            {
                output = Path.GetFullPath(serverOutputPath);
            }

            var missing = MissingBuildOutput(output);
            if (missing != null)
                return Fail($"The build output is incomplete: {missing} is missing.");

            var generationId = Guid.NewGuid();
            var candidate = Publish(output, layout, generationId);
            _out.WriteLine($"Published candidate {ReloadLayout.IdText(generationId)}: {output} -> {candidate}");

            Signal(layout, generationId);
            var timeout = TimeSpan.FromSeconds(timeoutSeconds > 0 ? timeoutSeconds : DefaultTimeoutSeconds);
            _out.WriteLine($"Signalled {layout.SignalPath}; waiting up to {timeout.TotalSeconds:F0} s for {layout.ReceiptPath(generationId)}.");

            var receipt = WaitForReceipt(layout, generationId, timeout, _pollInterval,
                received => _out.WriteLine($"{received.Status}: {ReloadReceiptFiles.Describe(received)}"));
            if (receipt == null)
                return Fail($"No final receipt within {timeout.TotalSeconds:F0} s. Is the server running with SWLOR_HOT_RELOAD=true in a dev or " +
                            "test environment, past its module load? Its log says 'reload-watching' once it watches the signal.");

            if (receipt.Status != ReloadReceiptStatus.Applied)
                return 1;

            foreach (var removed in Prune(layout, CandidatesKept))
                _out.WriteLine($"Removed old candidate {removed}");
            return 0;
        }

        /// <summary>
        /// Copies <paramref name="output"/> into a new candidate folder for <paramref name="generationId"/>, through a
        /// temporary folder beside it, so the server never sees a half-copied candidate. Returns the candidate folder.
        /// </summary>
        public static string Publish(string output, ReloadLayout layout, Guid generationId)
        {
            var target = layout.CandidateDirectory(generationId);
            if (Directory.Exists(target))
                throw new IOException($"{target} already exists; a candidate is published once.");

            Directory.CreateDirectory(layout.GenerationsDirectory);
            var partial = Path.Combine(layout.GenerationsDirectory, $".{ReloadLayout.IdText(generationId)}.partial");
            try
            {
                CopyAll(new DirectoryInfo(output), new DirectoryInfo(partial));
                Directory.Move(partial, target);
            }
            catch
            {
                if (Directory.Exists(partial))
                    Directory.Delete(partial, true);
                throw;
            }

            return target;
        }

        /// <summary>
        /// Writes the reload request naming <paramref name="generationId"/> (the hosting package's request format) to the
        /// signal file, through a temporary file moved over it, so the server never reads half a request.
        /// </summary>
        public static void Signal(ReloadLayout layout, Guid generationId)
        {
            Directory.CreateDirectory(layout.ReloadDirectory);
            var temporary = Path.Combine(layout.ReloadDirectory, $".{ReloadLayout.SignalFileName}.{Guid.NewGuid():N}.tmp");
            try
            {
                DevelopmentReloadFiles.WriteRequestAsync(
                        temporary,
                        new DevelopmentReloadRequest(DevelopmentReloadFiles.CurrentFormatVersion, generationId),
                        CancellationToken.None)
                    .GetAwaiter().GetResult();
                File.Move(temporary, layout.SignalPath, overwrite: true);
            }
            finally
            {
                File.Delete(temporary);
            }
        }

        /// <summary>
        /// Polls the receipt of <paramref name="generationId"/> until it is final or <paramref name="timeout"/> passes;
        /// <paramref name="onReceipt"/> sees each new status. Returns the final receipt, or null on a timeout.
        /// </summary>
        public static ReloadReceipt WaitForReceipt(
            ReloadLayout layout,
            Guid generationId,
            TimeSpan timeout,
            TimeSpan pollInterval,
            Action<ReloadReceipt> onReceipt)
        {
            var path = layout.ReceiptPath(generationId);
            var elapsed = Stopwatch.StartNew();
            ReloadReceiptStatus? seen = null;
            while (true)
            {
                var receipt = TryReadReceipt(path);
                if (receipt != null && receipt.GenerationId == generationId && receipt.Status != seen)
                {
                    seen = receipt.Status;
                    onReceipt(receipt);
                    if (receipt.IsFinal)
                        return receipt;
                }

                if (elapsed.Elapsed >= timeout)
                    return null;
                Thread.Sleep(pollInterval);
            }
        }

        /// <summary>
        /// Removes candidate folders beyond the newest <paramref name="keep"/>. Called after an applied reload, when the
        /// newest candidate is the running build and every older one is retired. Returns the folders removed.
        /// </summary>
        public static IReadOnlyList<string> Prune(ReloadLayout layout, int keep)
        {
            var generations = new DirectoryInfo(layout.GenerationsDirectory);
            if (!generations.Exists)
                return Array.Empty<string>();

            var removed = new List<string>();
            var stale = generations.GetDirectories()
                .Where(directory => Guid.TryParseExact(directory.Name, "N", out _))
                .OrderByDescending(directory => directory.LastWriteTimeUtc)
                .Skip(keep);
            foreach (var directory in stale)
            {
                try
                {
                    directory.Delete(true);
                    removed.Add(directory.FullName);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // A retired build's files can stay mapped for a while; the next applied reload tries again.
                }
            }

            return removed;
        }

        /// <summary>The repository root (the folder of SWLOR.Game.Server.sln) at or above <paramref name="directory"/>, or null.</summary>
        public static string FindRepositoryRoot(string directory)
        {
            for (var current = new DirectoryInfo(Path.GetFullPath(directory)); current != null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "SWLOR.Game.Server.sln")))
                    return current.FullName;
            }

            return null;
        }

        private static string MissingBuildOutput(string output)
        {
            foreach (var name in new[] { ReloadLayout.GameAssemblyFileName, "SWLOR.Game.Server.deps.json", "SWLOR.Game.Server.runtimeconfig.json" })
            {
                var path = Path.Combine(output, name);
                if (!File.Exists(path))
                    return path;
            }

            return null;
        }

        private static ReloadReceipt TryReadReceipt(string path)
        {
            try
            {
                return ReloadReceiptFiles.Read(path);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                // The server may be replacing it; the next poll reads it whole.
                return null;
            }
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);
            foreach (var file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), false);
            foreach (var directory in source.GetDirectories())
                CopyAll(directory, new DirectoryInfo(Path.Combine(target.FullName, directory.Name)));
        }

        private int Fail(string message)
        {
            _out.WriteLine("error : " + message);
            return 1;
        }

        private static int BuildRelease(string project)
        {
            var info = new ProcessStartInfo("dotnet") { UseShellExecute = false };
            foreach (var argument in new[] { "build", project, "-c", "Release", "-p:RunPostBuildEvent=Never", "-nologo", "-v", "q", "-clp:ErrorsOnly" })
                info.ArgumentList.Add(argument);
            using var process = System.Diagnostics.Process.Start(info);
            process.WaitForExit();
            return process.ExitCode;
        }
    }
}
