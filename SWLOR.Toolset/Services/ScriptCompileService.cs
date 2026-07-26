using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Script.Compile;
using SWLOR.Toolset.Domain.Script.Syntax;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// Drives the vendored NWScript compiler for the app: compile-on-save, Build All Scripts, and
    /// the staleness scan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Compilation matters here in a way it would not in another toolset: <c>ModulePacker</c> copies
    /// <c>./nss/</c> and <c>./ncs/</c> verbatim and never compiles, so the committed <c>.ncs</c> is
    /// what the game runs. An editor that saved source without rebuilding bytecode would be a trap.
    /// </para>
    /// <para>
    /// The engine header is staged into a temp directory as <c>nwscript.nss</c> because the compiler
    /// resolves its language spec under that plain name, while the in-repo copy is version-stamped.
    /// </para>
    /// </remarks>
    public sealed class ScriptCompileService
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly OutputLogService _log;
        private readonly ToolsetSettings? _settings;
        private string? _stagedHeaderDirectory;

        public ScriptCompileService(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            ToolsetSettings? settings = null)
        {
            _workspaceContext = workspaceContext;
            _log = log;
            _settings = settings;
        }

        /// <summary>Where the vendored compiler lives, beside nwn_gff.exe and nwn_erf.exe.</summary>
        public string? CompilerPath
        {
            get
            {
                var root = RepositoryRoot();
                if (root == null)
                    return null;

                var path = Path.Combine(root, "tools", "SWLOR.CLI", "nwn_script_comp.exe");
                return File.Exists(path) ? path : null;
            }
        }

        public bool IsAvailable => CompilerPath != null;

        /// <summary>Compiles one script to Module/ncs. Returns true when the artifact was written.</summary>
        public async Task<bool> CompileAsync(string resRef, CancellationToken cancellationToken = default)
        {
            var workspace = _workspaceContext.Workspace;
            var compiler = CreateCompiler();
            if (workspace == null || compiler == null)
            {
                _log.AppendLine("Cannot compile: no module open, or nwn_script_comp.exe is missing from tools/SWLOR.CLI.");
                return false;
            }

            var source = workspace.GetResourcePath(ResourceType.Nss, resRef);
            if (!File.Exists(source))
            {
                _log.AppendLine($"Cannot compile {resRef}: source not found.");
                return false;
            }

            // Includes declare no main() and produce no artifact; compiling one is not a failure but
            // there is nothing to write, so skip straight to reporting what it invalidated.
            if (!ScriptStalenessScanner.IsEntryPoint(ScriptTextDocument.Load(source).Text))
            {
                var dependents = IncludeGraph()?.TransitiveDependents(resRef) ?? Array.Empty<string>();
                _log.AppendLine(dependents.Count == 0
                    ? $"{resRef} is an include and has no compiled output."
                    : $"{resRef} is an include; {dependents.Count} dependent script(s) now need recompiling.");
                return true;
            }

            var ncsDirectory = Path.Combine(workspace.ModuleRoot, "ncs");
            Directory.CreateDirectory(ncsDirectory);
            var output = Path.Combine(ncsDirectory, resRef + ".ncs");

            var result = await compiler.CompileAsync(source, output, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                _log.AppendLine($"Compiled {resRef}.nss -> ncs/{resRef}.ncs");
                return true;
            }

            _log.AppendLine(ScriptCompiler.RequiresGameInstall(result)
                ? $"Could not compile {resRef}: it includes base-game headers, which needs an NWN installation."
                : $"Could not compile {resRef}.");

            foreach (var diagnostic in result.Diagnostics)
                _log.AppendLine($"  {diagnostic.File}({diagnostic.Line}): {diagnostic.Message}");

            return false;
        }

        /// <summary>Compile-checks one script without writing, for diagnostics.</summary>
        public async Task<IReadOnlyList<ScriptAnalysisDiagnostic>> CheckAsync(
            string resRef, CancellationToken cancellationToken = default)
        {
            var workspace = _workspaceContext.Workspace;
            var compiler = CreateCompiler();
            if (workspace == null || compiler == null)
                return Array.Empty<ScriptAnalysisDiagnostic>();

            var source = workspace.GetResourcePath(ResourceType.Nss, resRef);
            if (!File.Exists(source))
                return Array.Empty<ScriptAnalysisDiagnostic>();

            var result = await compiler.CompileAsync(source, checkOnly: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return result.Diagnostics
                .Select(d => new ScriptAnalysisDiagnostic(
                    d.Message, 0, 0,
                    d.IsError ? ScriptDiagnosticSeverity.Error : ScriptDiagnosticSeverity.Warning,
                    ScriptDiagnosticSource.Compiler,
                    d.Line))
                .ToList();
        }

        /// <summary>Compiles every entry-point script in the module.</summary>
        public async Task<(int Compiled, int Failed)> BuildAllAsync(CancellationToken cancellationToken = default)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || !IsAvailable)
            {
                _log.AppendLine("Cannot build scripts: no module open, or the compiler is missing.");
                return (0, 0);
            }

            var compiled = 0;
            var failed = 0;

            foreach (var resRef in workspace.EnumerateResRefs(ResourceType.Nss))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var source = workspace.GetResourcePath(ResourceType.Nss, resRef);
                if (!File.Exists(source) || !ScriptStalenessScanner.IsEntryPoint(ScriptTextDocument.Load(source).Text))
                    continue;

                if (await CompileAsync(resRef, cancellationToken).ConfigureAwait(false))
                    compiled++;
                else
                    failed++;
            }

            _log.AppendLine($"Build All Scripts: {compiled} compiled, {failed} failed.");
            return (compiled, failed);
        }

        /// <summary>Every compiled script that would ship stale.</summary>
        public IReadOnlyList<StaleScript> ScanStale()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<StaleScript>();

            return new ScriptStalenessScanner(
                Path.Combine(workspace.ModuleRoot, "nss"),
                Path.Combine(workspace.ModuleRoot, "ncs")).Scan();
        }

        private ScriptIncludeGraph? IncludeGraph()
        {
            var workspace = _workspaceContext.Workspace;
            return workspace == null ? null : ScriptIncludeGraph.Build(Path.Combine(workspace.ModuleRoot, "nss"));
        }

        private ScriptCompiler? CreateCompiler()
        {
            var workspace = _workspaceContext.Workspace;
            var compilerPath = CompilerPath;
            if (workspace == null || compilerPath == null)
                return null;

            var directories = new List<string> { Path.Combine(workspace.ModuleRoot, "nss") };

            var staged = StageEngineHeader();
            if (staged != null)
                directories.Add(staged);

            // 16 of the module's scripts include base-game headers that live only in the install's
            // KEY/BIF. With a root they resolve; without one they fail, and CompileAsync says so
            // in those words rather than surfacing a bare FILE NOT FOUND.
            string? root = null;
            try
            {
                root = NwnInstallLocator.Locate(
                    string.IsNullOrWhiteSpace(_settings?.NwnInstallOverride) ? null : _settings.NwnInstallOverride);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not locate an NWN installation: {ex.Message}");
            }

            return new ScriptCompiler(compilerPath, directories, root);
        }

        /// <summary>Copies the version-stamped header to a temp dir under the name the compiler expects.</summary>
        private string? StageEngineHeader()
        {
            if (_stagedHeaderDirectory != null && File.Exists(Path.Combine(_stagedHeaderDirectory, "nwscript.nss")))
                return _stagedHeaderDirectory;

            var root = RepositoryRoot();
            if (root == null)
                return null;

            var headerDirectory = Path.Combine(root, "SWLOR.NWN.API", "NWN");
            if (!Directory.Exists(headerDirectory))
                return null;

            var header = Directory.EnumerateFiles(headerDirectory, "nwscript*.nss")
                .OrderByDescending(f => f, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (header == null)
                return null;

            try
            {
                var staging = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset", "nsscomp");
                Directory.CreateDirectory(staging);
                File.Copy(header, Path.Combine(staging, "nwscript.nss"), overwrite: true);
                _stagedHeaderDirectory = staging;
                return staging;
            }
            catch (IOException ex)
            {
                _log.AppendLine($"Could not stage the NWScript header: {ex.Message}");
                return null;
            }
        }

        private string? RepositoryRoot()
        {
            var start = _workspaceContext.Workspace?.ModuleRoot ?? AppContext.BaseDirectory;
            var current = new DirectoryInfo(start);

            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "tools", "SWLOR.CLI")))
                    return current.FullName;

                current = current.Parent;
            }

            return null;
        }
    }
}
