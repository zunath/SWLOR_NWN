using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace SWLOR.Toolset.Domain.Script.Compile
{
    /// <summary>One message from the compiler, mapped onto a file position.</summary>
    /// <param name="File">Source file the message refers to.</param>
    /// <param name="Line">1-based line, or 0 when the message names none.</param>
    /// <param name="Message">The text after the severity.</param>
    /// <param name="IsError">False for warnings.</param>
    public sealed record ScriptDiagnostic(string File, int Line, string Message, bool IsError);

    /// <summary>The outcome of one compile.</summary>
    /// <param name="Succeeded">True when the compiler exited cleanly.</param>
    /// <param name="Diagnostics">Errors and warnings, in the order reported.</param>
    /// <param name="Output">Raw combined stdout/stderr, for the Output panel.</param>
    /// <param name="Skipped">True when the file has no <c>main()</c> and so produces no .ncs.</param>
    public sealed record ScriptCompileResult(
        bool Succeeded, IReadOnlyList<ScriptDiagnostic> Diagnostics, string Output, bool Skipped)
    {
        public bool HasErrors => Diagnostics.Any(d => d.IsError);
    }

    /// <summary>
    /// Compiles NWScript by shelling out to the vendored <c>nwn_script_comp</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the authoritative half of the plan's two-tier diagnostics rule. The in-editor lexer
    /// and completion engine are advisory and deliberately conservative; the real compiler decides
    /// what is valid, and its output is what produces the <c>.ncs</c> the game actually runs.
    /// Writing a compiler in C# was rejected: codegen must match the official one exactly or bugs
    /// surface as gameplay rather than as build failures.
    /// </para>
    /// <para>
    /// The vendored binary wraps the official Beamdog compiler library and was gated by recompiling
    /// the module: 65 of 68 entry-point scripts came back byte-identical to the committed artifacts,
    /// the three exceptions being a one-ULP float rounding difference on the literal 1.9.
    /// </para>
    /// <para>
    /// <b>An NWN install is required for some scripts.</b> <c>nw_i0_generic</c> and the scripts derived
    /// from it include 14 base-game headers that live only in the install's KEY/BIF, so 16 of the 87
    /// module scripts cannot compile without <c>--root</c>. The rest compile from the staged engine
    /// header alone. <see cref="RequiresGameInstall"/> reports which situation a failure was.
    /// </para>
    /// </remarks>
    public sealed class ScriptCompiler
    {
        // "path.nss(23): ERROR: message" and the NSS(line) form the compiler uses for includes.
        private static readonly Regex DiagnosticPattern = new(
            @"^\s*(?<file>.+?\.nss)\((?<line>\d+)\)\s*:\s*(?<sev>ERROR|WARNING)\s*:\s*(?<msg>.*)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private readonly string _compilerPath;
        private readonly string? _gameRoot;
        private readonly IReadOnlyList<string> _sourceDirectories;

        /// <param name="compilerPath">Full path to nwn_script_comp.exe.</param>
        /// <param name="sourceDirectories">Directories added to the resource manager for include lookup.</param>
        /// <param name="gameRoot">NWN installation, or null to compile without base-game resources.</param>
        public ScriptCompiler(string compilerPath, IReadOnlyList<string> sourceDirectories, string? gameRoot = null)
        {
            _compilerPath = compilerPath;
            _sourceDirectories = sourceDirectories;
            _gameRoot = gameRoot;
        }

        /// <summary>True when the vendored compiler is present and runnable.</summary>
        public bool IsAvailable => File.Exists(_compilerPath);

        /// <summary>
        /// Compiles one script. With <paramref name="checkOnly"/> the compiler runs in <c>-s</c>
        /// simulate mode: it reports errors but writes nothing, which is what a pre-save check wants.
        /// </summary>
        public async Task<ScriptCompileResult> CompileAsync(
            string sourcePath,
            string? outputPath = null,
            bool checkOnly = false,
            CancellationToken cancellationToken = default)
        {
            if (!IsAvailable)
                return new ScriptCompileResult(false, Array.Empty<ScriptDiagnostic>(),
                    $"Script compiler not found at {_compilerPath}.", false);

            var args = new List<string>();
            if (_gameRoot != null)
            {
                args.Add("--root");
                args.Add(_gameRoot);
            }
            else
            {
                args.Add("--no-keys");
            }

            if (_sourceDirectories.Count > 0)
            {
                args.Add("--dirs");
                args.Add(string.Join(",", _sourceDirectories));
            }

            if (checkOnly)
                args.Add("-s");
            else if (outputPath != null)
            {
                args.Add("-o");
                args.Add(outputPath);
            }

            args.Add(sourcePath);

            var (exitCode, output) = await RunAsync(args, cancellationToken).ConfigureAwait(false);

            // Exit code 623 / "skipped" is the compiler's way of saying the file declares no main()
            // and so is an include. That is a normal outcome, not a failure.
            var skipped = output.Contains("1 skipped", StringComparison.OrdinalIgnoreCase) &&
                !output.Contains("1 successful", StringComparison.OrdinalIgnoreCase);

            var diagnostics = ParseDiagnostics(output);
            var succeeded = (exitCode == 0 || skipped) && !diagnostics.Any(d => d.IsError);

            return new ScriptCompileResult(succeeded, diagnostics, output, skipped);
        }

        /// <summary>
        /// True when this failure looks like a missing NWN install rather than a code error — the
        /// base-game include chain is unresolvable without one, and saying so beats a bare
        /// "FILE NOT FOUND".
        /// </summary>
        public static bool RequiresGameInstall(ScriptCompileResult result) =>
            result.Diagnostics.Any(d =>
                d.Message.Contains("FILE NOT FOUND", StringComparison.OrdinalIgnoreCase)) ||
            result.Output.Contains("FILE NOT FOUND", StringComparison.OrdinalIgnoreCase);

        /// <summary>Extracts positioned errors and warnings from raw compiler output.</summary>
        public static IReadOnlyList<ScriptDiagnostic> ParseDiagnostics(string output)
        {
            var list = new List<ScriptDiagnostic>();

            foreach (var raw in output.Split('\n'))
            {
                var match = DiagnosticPattern.Match(raw);
                if (!match.Success)
                    continue;

                list.Add(new ScriptDiagnostic(
                    match.Groups["file"].Value,
                    int.TryParse(match.Groups["line"].Value, out var line) ? line : 0,
                    match.Groups["msg"].Value.Trim(),
                    match.Groups["sev"].Value.Equals("ERROR", StringComparison.OrdinalIgnoreCase)));
            }

            return list;
        }

        private async Task<(int ExitCode, string Output)> RunAsync(
            IReadOnlyList<string> args, CancellationToken cancellationToken)
        {
            var info = new ProcessStartInfo
            {
                FileName = _compilerPath,
                WorkingDirectory = Path.GetDirectoryName(_compilerPath) ?? ".",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in args)
                info.ArgumentList.Add(arg);

            using var process = new Process { StartInfo = info };
            var output = new StringBuilder();

            process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (output) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (output) output.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await WaitForExitOrKillAsync(process, cancellationToken).ConfigureAwait(false);

            lock (output)
                return (process.ExitCode, output.ToString());
        }

        private static async Task WaitForExitOrKillAsync(
            Process process,
            CancellationToken cancellationToken)
        {
            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                }
                catch
                {
                    // Preserve the cancellation or wait failure that caused cleanup. A failed kill
                    // cannot make that original compiler outcome more useful to the caller.
                }

                throw;
            }
        }
    }
}
