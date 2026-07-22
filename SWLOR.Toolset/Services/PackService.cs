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

                if (process.ExitCode == 0)
                    DeployToDebugServer(repoRoot, moduleRoot, moduleFileName);

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
        /// Copies the freshly packed .mod into debugserver\modules so an in-app pack is playable
        /// without a separate deploy step. The CLI's pack (-p) deliberately does NOT do this —
        /// only its full deploy (-o, DeployBuild) copies the module, along with binaries and haks
        /// the toolset has no business rebuilding. Skipped quietly (with a hint) when the
        /// debugserver directory doesn't exist.
        /// </summary>
        private void DeployToDebugServer(string repoRoot, string moduleRoot, string moduleFileName)
        {
            try
            {
                var modulesDirectory = Path.Combine(repoRoot, "debugserver", "modules");
                if (!Directory.Exists(modulesDirectory))
                {
                    _log.AppendLine(
                        "debugserver\\modules not found - skipping deploy copy (run the CLI's full deploy (-o) once to create it).");
                    return;
                }

                var source = Path.Combine(moduleRoot, moduleFileName);
                var destination = Path.Combine(modulesDirectory, moduleFileName);
                File.Copy(source, destination, overwrite: true);
                _log.AppendLine($"Deployed '{moduleFileName}' to debugserver\\modules.");
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Pack succeeded but the debugserver copy failed: {ex.Message}");
            }
        }

        /// <summary>Finds a solution-built CLI that supports the required --no-prompt option.</summary>
        internal static string? ResolveCli(string repoRoot)
        {
            foreach (var configuration in new[] { "Debug", "Release" })
            {
                var built = Path.Combine(repoRoot, "SWLOR.CLI", "bin", configuration, "net10.0", "SWLOR.CLI.exe");
                if (File.Exists(built))
                    return built;
            }

            return null;
        }

        internal static string ReadModuleFileName(string moduleRoot)
        {
            return ModuleFileNameResolver.Read(moduleRoot);
        }
    }
}
