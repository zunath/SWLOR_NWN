using System.Diagnostics;
using System.Text.Json;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// Packs the module by invoking the existing SWLOR.CLI pipeline (which shells out to
    /// nwn_gff/nwn_erf), streaming its output to the Output panel. Prefers the freshly built
    /// CLI from the solution (which understands --no-prompt); falls back to the committed
    /// tools\SWLOR.CLI binary with redirected stdin when no built copy exists.
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

                var (cliPath, supportsNoPrompt) = ResolveCli(repoRoot);
                if (cliPath == null)
                {
                    _log.AppendLine("SWLOR.CLI.exe not found (looked in SWLOR.CLI\\bin and tools\\SWLOR.CLI).");
                    return -1;
                }

                var moduleFileName = ReadModuleFileName(moduleRoot);
                var arguments = supportsNoPrompt
                    ? $"-p \"./{moduleFileName}\" --no-prompt"
                    : $"-p \"./{moduleFileName}\"";

                _log.AppendLine($"Packing '{moduleFileName}' via {cliPath}...");
                var stopwatch = Stopwatch.StartNew();

                var startInfo = new ProcessStartInfo(cliPath, arguments)
                {
                    WorkingDirectory = moduleRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
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
                process.StandardInput.Close();
                await process.WaitForExitAsync().ConfigureAwait(false);

                stopwatch.Stop();
                _log.AppendLine($"Pack finished with exit code {process.ExitCode} in {stopwatch.ElapsedMilliseconds}ms.");
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

        /// <summary>Prefers the solution-built CLI (has --no-prompt); falls back to the
        /// committed tools binary, which predates the flag.</summary>
        internal static (string? Path, bool SupportsNoPrompt) ResolveCli(string repoRoot)
        {
            foreach (var configuration in new[] { "Debug", "Release" })
            {
                var built = Path.Combine(repoRoot, "SWLOR.CLI", "bin", configuration, "net8.0", "SWLOR.CLI.exe");
                if (File.Exists(built))
                    return (built, true);
            }

            var tools = Path.Combine(repoRoot, "tools", "SWLOR.CLI", "SWLOR.CLI.exe");
            return File.Exists(tools) ? (tools, false) : (null, false);
        }

        internal static string ReadModuleFileName(string moduleRoot)
        {
            // config.json is only trusted when the file it names actually exists — it has been
            // stale before (it still said "Star Wars LOR.mod" long after the v2 rename), and
            // packing to a wrong name silently breaks the deploy pipeline, which copies the
            // v2 file by name.
            try
            {
                var configPath = Path.Combine(moduleRoot, "config.json");
                if (File.Exists(configPath))
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
                    if (document.RootElement.TryGetProperty("ModuleFileName", out var name) &&
                        name.GetString() is { Length: > 0 } value &&
                        File.Exists(Path.Combine(moduleRoot, value)))
                    {
                        return value;
                    }
                }
            }
            catch (Exception)
            {
                // Fall through to the on-disk probe.
            }

            var existing = Directory.EnumerateFiles(moduleRoot, "*.mod")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
            if (existing != null)
                return Path.GetFileName(existing);

            return "Star Wars LOR v2.mod";
        }
    }
}
