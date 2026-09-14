using System.Diagnostics;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// Packs the module by rebuilding and invoking the solution SWLOR.CLI pipeline (which shells
    /// out to nwn_gff/nwn_erf), streaming its output to the Output panel. The CLI build also rebuilds
    /// its SWLOR.Game.Server project reference so embedded conversation graphs stay current.
    /// </summary>
    public sealed class PackService
    {
        private const string DeploymentTransactionPrefix = ".swlor-toolset-debug-deploy-";
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

                // The previous process may have exited between companion moves in a logical
                // resource delete. Recover that transaction on a worker before either MSBuild or
                // the CLI enumerates the module and conversation source trees.
                var recoveredDeletes = await Task.Run(
                        () => ModuleResourceDeletionService.RecoverInterruptedDeletes(moduleRoot))
                    .ConfigureAwait(false);
                foreach (var recovered in recoveredDeletes)
                    _log.AppendLine($"Recovered {recovered} from an interrupted delete before packing.");

                // Conversation saves use this same cross-process key. Hold it before MSBuild reads
                // the graph files until the matching module/server generation has been deployed, so
                // another editor cannot successfully save a graph that the just-built assembly lacks.
                var conversationDataRoot = ModuleWorkspace.ResolveConversationDataRoot(moduleRoot);
                using var conversationSourceLock = await Task.Run(
                        () => ModuleWriteLock.Acquire(conversationDataRoot))
                    .ConfigureAwait(false);

                var cliBuildExitCode = await BuildCliAsync(repoRoot).ConfigureAwait(false);
                if (cliBuildExitCode != 0)
                {
                    _log.AppendLine(
                        $"Pack stopped because the CLI build failed with exit code {cliBuildExitCode}.");
                    return cliBuildExitCode;
                }

                var cliPath = ResolveCli(repoRoot);
                if (cliPath == null)
                {
                    _log.AppendLine("The SWLOR.CLI build succeeded but its executable was not found.");
                    return -1;
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
        /// Rebuilds the CLI from the current checkout before packing. Its server project reference
        /// also refreshes embedded conversation graphs. The post-build event is disabled explicitly
        /// to avoid recursively invoking the CLI deploy.
        /// </summary>
        private async Task<int> BuildCliAsync(string repoRoot)
        {
            var projectPath = Path.Combine(repoRoot, "SWLOR.CLI", "SWLOR.CLI.csproj");
            if (!File.Exists(projectPath))
            {
                _log.AppendLine($"Cannot pack: CLI project not found at {projectPath}.");
                return -1;
            }

            _log.AppendLine("Building SWLOR.CLI and its server dependency before packing...");
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
                    _log.AppendLine($"[cli-build] {e.Data}");
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    _log.AppendLine($"[cli-build:err] {e.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync().ConfigureAwait(false);
            return process.ExitCode;
        }

        /// <summary>
        /// Stages the packed module and rebuilt server output, then installs them as one generation.
        /// If either installation fails, the previous generation is restored before returning.
        /// </summary>
        private bool DeployToDebugServer(string repoRoot, string moduleRoot, string moduleFileName)
        {
            var transactionRoot = string.Empty;
            try
            {
                var debugServerRoot = Path.Combine(repoRoot, "debugserver");
                var modulesDirectory = Path.Combine(debugServerRoot, "modules");
                if (!Directory.Exists(modulesDirectory))
                {
                    _log.AppendLine(
                        "debugserver\\modules not found - skipping deploy copy (run the CLI's full deploy (-o) once to create it).");
                    return true;
                }

                var source = Path.Combine(moduleRoot, moduleFileName);
                var destination = Path.Combine(modulesDirectory, moduleFileName);
                var serverOutput = Path.Combine(
                    repoRoot, "SWLOR.Game.Server", "bin", "Debug", "net10.0");
                if (!Directory.Exists(serverOutput))
                    throw new DirectoryNotFoundException(
                        $"The server build output was not found at {serverOutput}.");

                var dotnetDirectory = Path.Combine(debugServerRoot, "dotnet");
                transactionRoot = Path.Combine(
                    debugServerRoot,
                    DeploymentTransactionPrefix + Guid.NewGuid().ToString("N"));
                var stagedModule = Path.Combine(
                    transactionRoot, "staged", "modules", moduleFileName);
                var stagedDotnet = Path.Combine(transactionRoot, "staged", "dotnet");
                var backupModule = Path.Combine(
                    transactionRoot, "backup", "modules", moduleFileName);
                var backupDotnet = Path.Combine(transactionRoot, "backup", "dotnet");

                Directory.CreateDirectory(Path.GetDirectoryName(stagedModule)!);
                File.Copy(source, stagedModule);
                CopyDirectory(serverOutput, stagedDotnet);

                var hadModule = File.Exists(destination);
                var hadDotnet = Directory.Exists(dotnetDirectory);
                var installedModule = false;
                var installedDotnet = false;

                try
                {
                    if (hadModule)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(backupModule)!);
                        File.Move(destination, backupModule);
                    }

                    if (hadDotnet)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(backupDotnet)!);
                        Directory.Move(dotnetDirectory, backupDotnet);
                    }

                    File.Move(stagedModule, destination);
                    installedModule = true;
                    Directory.Move(stagedDotnet, dotnetDirectory);
                    installedDotnet = true;
                }
                catch (Exception installException)
                {
                    var rollbackErrors = new List<string>();
                    if (installedDotnet)
                        TryRollback(
                            () => Directory.Delete(dotnetDirectory, recursive: true),
                            "remove the partially installed server output",
                            rollbackErrors);
                    if (hadDotnet && Directory.Exists(backupDotnet))
                        TryRollback(
                            () => Directory.Move(backupDotnet, dotnetDirectory),
                            "restore the previous server output",
                            rollbackErrors);
                    if (installedModule)
                        TryRollback(
                            () => File.Delete(destination),
                            "remove the partially installed module",
                            rollbackErrors);
                    if (hadModule && File.Exists(backupModule))
                        TryRollback(
                            () => File.Move(backupModule, destination),
                            "restore the previous module",
                            rollbackErrors);

                    if (rollbackErrors.Count == 0)
                        TryDeleteTransactionDirectory(transactionRoot);
                    else
                        _log.AppendLine(
                            $"Deployment rollback needs manual recovery from '{transactionRoot}': " +
                            string.Join("; ", rollbackErrors));

                    _log.AppendLine(
                        $"Pack succeeded but the debugserver deployment was rolled back: {installException.Message}");
                    return false;
                }

                TryDeleteTransactionDirectory(transactionRoot);
                _log.AppendLine($"Deployed '{moduleFileName}' to debugserver\\modules.");
                _log.AppendLine("Deployed the rebuilt server assembly to debugserver\\dotnet.");
                return true;
            }
            catch (Exception ex)
            {
                TryDeleteTransactionDirectory(transactionRoot);
                _log.AppendLine($"Pack succeeded but the debugserver staging failed: {ex.Message}");
                return false;
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.EnumerateFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);

            foreach (var directory in Directory.EnumerateDirectories(source))
            {
                var childDestination = Path.Combine(destination, Path.GetFileName(directory));
                CopyDirectory(directory, childDestination);
            }
        }

        private static void TryRollback(
            Action rollback,
            string description,
            ICollection<string> errors)
        {
            try
            {
                rollback();
            }
            catch (Exception ex)
            {
                errors.Add($"could not {description}: {ex.Message}");
            }
        }

        private void TryDeleteTransactionDirectory(string transactionRoot)
        {
            if (string.IsNullOrEmpty(transactionRoot) || !Directory.Exists(transactionRoot))
                return;

            try
            {
                Directory.Delete(transactionRoot, recursive: true);
            }
            catch (Exception ex)
            {
                _log.AppendLine(
                    $"Debugserver deployment cleanup left '{transactionRoot}': {ex.Message}");
            }
        }

        /// <summary>Finds the Debug CLI produced immediately before packing.</summary>
        internal static string? ResolveCli(string repoRoot)
        {
            var path = Path.Combine(repoRoot, "SWLOR.CLI", "bin", "Debug", "net10.0", "SWLOR.CLI.exe");
            return File.Exists(path) ? path : null;
        }

        internal static string ReadModuleFileName(string moduleRoot)
        {
            return ModuleFileNameResolver.Read(moduleRoot);
        }
    }
}
