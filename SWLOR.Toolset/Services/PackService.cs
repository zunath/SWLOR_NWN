using System.Diagnostics;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// Packs the module by invoking the existing SWLOR.CLI pipeline (which shells out to
    /// nwn_gff/nwn_erf), streaming its output to the Output panel. Only a solution-built CLI that
    /// supports --no-prompt is used; an older interactive tools binary can block or fail when
    /// launched without a console, so it is deliberately not a fallback.
    /// </summary>
    public sealed class PackService
    {
        private readonly OutputLogService _log;
        private int _isPacking;

        public PackService(OutputLogService log)
        {
            _log = log;
        }

        public bool IsPacking => _isPacking != 0;

        /// <summary>Runs a pack for the module under the given module root. Returns the CLI
        /// exit code, or -1 when the pack could not start (already running, CLI missing).</summary>
        public async Task<int> PackAsync(string moduleRoot)
        {
            if (Interlocked.CompareExchange(ref _isPacking, 1, 0) != 0)
            {
                _log.AppendLine("A pack is already running.");
                return -1;
            }

            try
            {
                var repoRoot = Directory.GetParent(moduleRoot)?.FullName;
                if (repoRoot == null)
                {
                    _log.AppendLine("Cannot determine repository root from the module root.");
                    return -1;
                }

                var cliPath = ResolveCli(repoRoot);
                if (cliPath == null)
                {
                    _log.AppendLine("A solution-built SWLOR.CLI.exe was not found. Build SWLOR.CLI before packing.");
                    return -1;
                }

                var serverBuildExitCode = await BuildServerAsync(repoRoot).ConfigureAwait(false);
                if (serverBuildExitCode != 0)
                {
                    _log.AppendLine(
                        $"Pack stopped because the server build failed with exit code {serverBuildExitCode}.");
                    return serverBuildExitCode;
                }

                var moduleFileName = ReadModuleFileName(moduleRoot);
                var arguments = $"-p \"./{moduleFileName}\" --no-prompt";

                _log.AppendLine($"Packing '{moduleFileName}' via {cliPath}...");
                var stopwatch = Stopwatch.StartNew();

                var startInfo = new ProcessStartInfo(cliPath, arguments)
                {
                    WorkingDirectory = moduleRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                        _log.AppendLine($"[pack] {e.Data}");
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                        _log.AppendLine($"[pack:err] {e.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync().ConfigureAwait(false);

                stopwatch.Stop();
                _log.AppendLine($"Pack finished with exit code {process.ExitCode} in {stopwatch.ElapsedMilliseconds}ms.");

                if (process.ExitCode == 0 &&
                    !DeployToDebugServer(repoRoot, moduleRoot, moduleFileName))
                    return -2;

                return process.ExitCode;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Pack failed: {ex.Message}");
                return -1;
            }
            finally
            {
                Interlocked.Exchange(ref _isPacking, 0);
            }
        }

        /// <summary>
        /// Rebuilds the server before packing. Conversation graphs are embedded resources, so a
        /// module-only pack would otherwise leave graph edits out of the playable assembly. The
        /// post-build event is disabled explicitly to avoid recursively invoking the CLI deploy.
        /// </summary>
        private async Task<int> BuildServerAsync(string repoRoot)
        {
            var projectPath = Path.Combine(repoRoot, "SWLOR.Game.Server", "SWLOR.Game.Server.csproj");
            if (!File.Exists(projectPath))
            {
                _log.AppendLine($"Cannot build conversation data: server project not found at {projectPath}.");
                return -1;
            }

            _log.AppendLine("Building SWLOR.Game.Server so conversation graph edits are embedded...");
            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("build");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("Debug");
            startInfo.ArgumentList.Add("-p:RunPostBuildEvent=Never");

            using var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    _log.AppendLine($"[server-build] {e.Data}");
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    _log.AppendLine($"[server-build:err] {e.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync().ConfigureAwait(false);
            return process.ExitCode;
        }

        /// <summary>Copies the packed module and rebuilt server output into debugserver.</summary>
        private bool DeployToDebugServer(string repoRoot, string moduleRoot, string moduleFileName)
        {
            try
            {
                var modulesDirectory = Path.Combine(repoRoot, "debugserver", "modules");
                if (!Directory.Exists(modulesDirectory))
                {
                    _log.AppendLine(
                        "debugserver\\modules not found - skipping deploy copy (run the CLI's full deploy (-o) once to create it).");
                    return true;
                }

                var source = Path.Combine(moduleRoot, moduleFileName);
                var destination = Path.Combine(modulesDirectory, moduleFileName);
                File.Copy(source, destination, overwrite: true);
                _log.AppendLine($"Deployed '{moduleFileName}' to debugserver\\modules.");

                var serverOutput = Path.Combine(
                    repoRoot, "SWLOR.Game.Server", "bin", "Debug", "net10.0");
                var dotnetDirectory = Path.Combine(repoRoot, "debugserver", "dotnet");
                if (!Directory.Exists(serverOutput))
                    throw new DirectoryNotFoundException(
                        $"The server build output was not found at {serverOutput}.");
                Directory.CreateDirectory(dotnetDirectory);
                CopyDirectory(serverOutput, dotnetDirectory);
                _log.AppendLine("Deployed the rebuilt server assembly to debugserver\\dotnet.");
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Pack succeeded but the debugserver copy failed: {ex.Message}");
                return false;
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            foreach (var file in Directory.EnumerateFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);

            foreach (var directory in Directory.EnumerateDirectories(source))
            {
                var childDestination = Path.Combine(destination, Path.GetFileName(directory));
                Directory.CreateDirectory(childDestination);
                CopyDirectory(directory, childDestination);
            }
        }

        /// <summary>Finds the newest solution-built CLI that supports the required --no-prompt option.</summary>
        internal static string? ResolveCli(string repoRoot)
        {
            return new[] { "Debug", "Release" }
                .Select(configuration =>
                    Path.Combine(repoRoot, "SWLOR.CLI", "bin", configuration, "net10.0", "SWLOR.CLI.exe"))
                .Where(File.Exists)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        internal static string ReadModuleFileName(string moduleRoot)
        {
            return ModuleFileNameResolver.Read(moduleRoot);
        }
    }
}
