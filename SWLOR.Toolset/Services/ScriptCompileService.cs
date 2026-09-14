using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.NWN.Formats.Common;
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
        private readonly string? _compilerPathOverride;

        public ScriptCompileService(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            ToolsetSettings? settings = null,
            string? compilerPathOverride = null)
        {
            _workspaceContext = workspaceContext;
            _log = log;
            _settings = settings;
            _compilerPathOverride = compilerPathOverride;
        }

        /// <summary>Where the vendored compiler lives, beside nwn_gff.exe and nwn_erf.exe.</summary>
        public string? CompilerPath
        {
            get
            {
                if (_compilerPathOverride != null)
                    return File.Exists(_compilerPathOverride) ? _compilerPathOverride : null;

                var root = RepositoryRoot();
                if (root == null)
                    return null;

                var path = Path.Combine(root, "tools", "SWLOR.CLI", "nwn_script_comp.exe");
                return File.Exists(path) ? path : null;
            }
        }

        public bool IsAvailable => CompilerPath != null;

        /// <summary>What a compile produced: whether it wrote, and what the compiler said.</summary>
        /// <param name="Succeeded">True when the artifact was written (or the file is an include).</param>
        /// <param name="Diagnostics">Compiler findings, mapped onto buffer offsets.</param>
        public sealed record CompileOutcome(
            bool Succeeded,
            IReadOnlyList<ScriptAnalysisDiagnostic> Diagnostics,
            int RebuiltDependents = 0)
        {
            public static CompileOutcome Failed(string _) =>
                new(false, Array.Empty<ScriptAnalysisDiagnostic>());

            public static CompileOutcome Ok() => new(true, Array.Empty<ScriptAnalysisDiagnostic>());
        }

        /// <summary>
        /// Raised after any compile, so the Problems panel can show the authoritative tier's findings
        /// alongside the editor's advisory ones.
        /// </summary>
        public event Action<string, IReadOnlyList<ScriptAnalysisDiagnostic>>? DiagnosticsProduced;

        /// <summary>
        /// Serializes every compiler run - tab compiles, dependent recompiles, and Build All -
        /// so two compiler processes can never produce or replace the same .ncs concurrently.
        /// Re-entrant within one logical compile flow via the same AsyncLocal pattern
        /// <see cref="ModuleMutationLock"/> uses.
        /// </summary>
        private static readonly SemaphoreSlim CompilerGate = new(1, 1);
        private static readonly AsyncLocal<int> CompilerGateDepth = new();
        private static int _activeCompilations;

        /// <summary>
        /// True while any compiler run (single script or Build All) is producing artifacts.
        /// Module-scoped operations (pack, validation, Build All entry) consult this so they never
        /// copy or rebuild an .ncs mid-replacement.
        /// </summary>
        public static bool AnyCompilationActive => Volatile.Read(ref _activeCompilations) > 0;

        private static async Task<T> WithCompilerGateAsync<T>(
            CancellationToken cancellationToken, Func<Task<T>> action)
        {
            if (CompilerGateDepth.Value > 0)
                return await action().ConfigureAwait(false);

            await CompilerGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            CompilerGateDepth.Value++;
            Interlocked.Increment(ref _activeCompilations);
            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _activeCompilations);
                CompilerGateDepth.Value--;
                CompilerGate.Release();
            }
        }

        /// <summary>Compiles one script to Module/ncs.</summary>
        public Task<CompileOutcome> CompileAsync(string resRef, CancellationToken cancellationToken = default) =>
            WithCompilerGateAsync(cancellationToken, () => CompileGatedAsync(resRef, cancellationToken));

        private async Task<CompileOutcome> CompileGatedAsync(string resRef, CancellationToken cancellationToken)
        {
            var workspace = _workspaceContext.Workspace;
            var compiler = CreateCompiler();
            if (workspace == null || compiler == null)
            {
                _log.AppendLine("Cannot compile: no module open, or nwn_script_comp.exe is missing from tools/SWLOR.CLI.");
                return CompileOutcome.Failed(resRef);
            }

            using var moduleWriteLock = ModuleWriteLock.Acquire(workspace.ModuleRoot);
            var source = workspace.GetResourcePath(ResourceType.Nss, resRef);
            if (!File.Exists(source))
            {
                _log.AppendLine($"Cannot compile {resRef}: source not found.");
                return CompileOutcome.Failed(resRef);
            }

            // Includes declare no main() and produce no artifact; compiling one is not a failure but
            // every entry point that depends on one must be rebuilt before a save or pack can report
            // success. ModulePacker copies existing .ncs files verbatim and never compiles them.
            if (!ScriptStalenessScanner.IsEntryPoint(ScriptTextDocument.Load(source).Text))
            {
                var dependents = IncludeGraph()?.TransitiveDependents(resRef) ?? Array.Empty<string>();
                var entryPoints = dependents
                    .Where(dependent => IsEntryPoint(workspace, dependent))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                _log.AppendLine(entryPoints.Count == 0
                    ? $"{resRef} is an include and has no compiled output."
                    : $"{resRef} is an include; rebuilding {entryPoints.Count} dependent script(s).");

                // An include still gets compile-checked, so a syntax error in a header is reported
                // where it was made rather than only in whichever dependent is rebuilt next.
                var checkResult = await compiler.CompileAsync(source, checkOnly: true, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                var checkDiagnostics = ToAnalysisDiagnostics(
                    checkResult, ScriptTextDocument.Load(source).Text, resRef);
                DiagnosticsProduced?.Invoke(resRef, checkDiagnostics);
                if (!checkResult.Succeeded)
                {
                    _log.AppendLine($"Could not compile-check include {resRef}; dependent scripts were not rebuilt.");
                    return new CompileOutcome(false, checkDiagnostics);
                }

                // A script that used to be an entry point may already have bytecode on disk. Once
                // its source becomes an include, that artifact is no longer buildable and must not
                // be packed as if the old behavior still existed.
                var obsoleteOutput = Path.Combine(workspace.ModuleRoot, "ncs", resRef + ".ncs");
                if (File.Exists(obsoleteOutput))
                {
                    File.Delete(obsoleteOutput);
                    _log.AppendLine($"Removed obsolete compiled output ncs/{resRef}.ncs.");
                }

                var failed = 0;
                foreach (var dependent in entryPoints)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!(await CompileAsync(dependent, cancellationToken).ConfigureAwait(false)).Succeeded)
                        failed++;
                }

                if (failed > 0)
                {
                    _log.AppendLine(
                        $"Include {resRef} was valid, but {failed} dependent script(s) failed to rebuild.");
                    return new CompileOutcome(false, checkDiagnostics);
                }

                if (entryPoints.Count > 0)
                    _log.AppendLine($"Rebuilt all {entryPoints.Count} script(s) affected by include {resRef}.");

                // Reported back rather than left implicit: the caller offered a second, identical
                // rebuild because it had no way to tell that this one had already happened.
                return new CompileOutcome(true, checkDiagnostics, entryPoints.Count);
            }

            var ncsDirectory = Path.Combine(workspace.ModuleRoot, "ncs");
            Directory.CreateDirectory(ncsDirectory);
            var output = Path.Combine(ncsDirectory, resRef + ".ncs");

            // Compiled to a transaction-unique temp file beside the canonical one - same directory, so
            // the replace below is a same-volume rename - rather than straight to output. If the
            // compiler crashes or is killed mid-write, a partial file lands on the temp path, not on
            // the canonical .ncs: ScriptStalenessScanner only compares timestamps, so a partial write
            // to the real path would look newer than its source and ModulePacker would ship it verbatim.
            // The name must still end in ".ncs" - nwn_script_comp derives its output resref from -o by
            // stripping whatever extension is there and reappending its own, so "foo.<guid>.ncs.tmp"
            // comes back out as "foo.<guid>.ncs.ncs" rather than landing on the path actually given.
            var temporaryOutput = Path.Combine(ncsDirectory, $"{resRef}.{Guid.NewGuid():N}.ncs");
            var compileInputs = CaptureCompileInputs(workspace, resRef);

            ScriptCompileResult result;
            try
            {
                result = await compiler.CompileAsync(source, temporaryOutput, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                DeleteTempOutput(temporaryOutput);
                throw;
            }

            var diagnostics = ToAnalysisDiagnostics(
                result, ScriptTextDocument.Load(source).Text, resRef);
            DiagnosticsProduced?.Invoke(resRef, diagnostics);

            if (result.Succeeded)
            {
                if (!CompileInputsAreUnchanged(compileInputs))
                {
                    DeleteTempOutput(temporaryOutput);
                    _log.AppendLine(
                        $"Did not install {resRef}.ncs because its source or an included script changed during compilation. Compile again.");
                    return new CompileOutcome(false, diagnostics);
                }

                // Installed only now that the compiler reported success - the previous valid artifact
                // is never visible in a partially-written state.
                File.Move(temporaryOutput, output, overwrite: true);
                new ScriptStalenessScanner(
                        Path.Combine(workspace.ModuleRoot, "nss"),
                        ncsDirectory,
                        CanResolveExternalInclude)
                    .RecordSuccessfulCompile(resRef);
                _log.AppendLine($"Compiled {resRef}.nss -> ncs/{resRef}.ncs");
                return new CompileOutcome(true, diagnostics);
            }

            DeleteTempOutput(temporaryOutput);

            _log.AppendLine(ScriptCompiler.RequiresGameInstall(result)
                ? $"Could not compile {resRef}: it includes base-game headers, which needs an NWN installation."
                : $"Could not compile {resRef}.");

            foreach (var diagnostic in result.Diagnostics)
                _log.AppendLine($"  {diagnostic.File}({diagnostic.Line}): {diagnostic.Message}");

            return new CompileOutcome(false, diagnostics);
        }

        private static IReadOnlyDictionary<string, byte[]?> CaptureCompileInputs(
            ModuleWorkspace workspace,
            string resRef)
        {
            var nssDirectory = Path.Combine(workspace.ModuleRoot, "nss");
            var inputs = new Dictionary<string, byte[]?>(StringComparer.OrdinalIgnoreCase);
            var pending = new Queue<(string ResRef, int Depth)>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            pending.Enqueue((resRef, 0));

            while (pending.Count > 0)
            {
                var (inputResRef, depth) = pending.Dequeue();
                if (!visited.Add(inputResRef))
                    continue;

                var path = Path.Combine(nssDirectory, inputResRef + ".nss");
                if (!File.Exists(path))
                {
                    inputs[path] = null;
                    continue;
                }

                var bytes = File.ReadAllBytes(path);
                inputs[path] = System.Security.Cryptography.SHA256.HashData(bytes);
                if (depth >= ScriptIncludeGraph.MaxIncludeDepth)
                    continue;

                foreach (var include in ScriptOutline.Build(
                             ScriptTextDocument.FromBytes(bytes).Text).Includes)
                {
                    pending.Enqueue((include, depth + 1));
                }
            }

            return inputs;
        }

        private static bool CompileInputsAreUnchanged(
            IReadOnlyDictionary<string, byte[]?> expected)
        {
            foreach (var (path, expectedHash) in expected)
            {
                var exists = File.Exists(path);
                if (expectedHash == null)
                {
                    if (exists)
                        return false;
                    continue;
                }

                if (!exists)
                    return false;

                try
                {
                    var currentHash = System.Security.Cryptography.SHA256.HashData(
                        File.ReadAllBytes(path));
                    if (!currentHash.AsSpan().SequenceEqual(expectedHash))
                        return false;
                }
                catch (IOException)
                {
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            }

            return true;
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

            return ToAnalysisDiagnostics(result, ScriptTextDocument.Load(source).Text, resRef);
        }

        /// <summary>
        /// Maps compiler output onto buffer offsets so its findings can be squiggled like the
        /// editor's own.
        /// </summary>
        /// <remarks>
        /// The compiler reports a line but no column, so the whole line's trimmed extent is
        /// underlined rather than a guessed word. A finding naming a <i>different</i> file — the
        /// compiler reports errors inside includes against the include's own name — gets zero length,
        /// which the squiggle renderer skips; it still lists in Problems, where the filename is
        /// visible and the position is meaningful.
        /// </remarks>
        public static IReadOnlyList<ScriptAnalysisDiagnostic> ToAnalysisDiagnostics(
            ScriptCompileResult result, string source, string sourceResRef)
        {
            var lineStarts = BuildLineIndex(source);

            return result.Diagnostics.Select(d =>
            {
                var diagnosticResRef = string.IsNullOrWhiteSpace(d.File)
                    ? sourceResRef
                    : Path.GetFileNameWithoutExtension(d.File);
                var belongsToSource = string.Equals(
                    diagnosticResRef, sourceResRef, StringComparison.OrdinalIgnoreCase);
                var (start, length) = belongsToSource
                    ? SpanForLine(source, lineStarts, d.Line)
                    : (0, 0);
                return new ScriptAnalysisDiagnostic(
                    d.Message, start, length,
                    d.IsError ? ScriptDiagnosticSeverity.Error : ScriptDiagnosticSeverity.Warning,
                    ScriptDiagnosticSource.Compiler,
                    d.Line,
                    belongsToSource ? null : diagnosticResRef);
            }).ToList();
        }

        private static List<int> BuildLineIndex(string source)
        {
            var starts = new List<int> { 0 };
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i] == '\n')
                    starts.Add(i + 1);
            }

            return starts;
        }

        private static (int Start, int Length) SpanForLine(string source, List<int> lineStarts, int line)
        {
            if (line < 1 || line > lineStarts.Count)
                return (0, 0);

            var start = lineStarts[line - 1];
            var end = line < lineStarts.Count ? lineStarts[line] - 1 : source.Length;
            if (end < start)
                return (start, 0);

            // Trim the indent so the underline starts at the code, not at column 1.
            while (start < end && char.IsWhiteSpace(source[start]))
                start++;

            while (end > start && char.IsWhiteSpace(source[end - 1]))
                end--;

            return (start, end - start);
        }

        /// <summary>What a Build All actually did.</summary>
        /// <param name="Ran">
        /// False when nothing could be attempted - no module open, or no vendored compiler. Zero
        /// compiled and zero failed is indistinguishable from a clean build without this, which is
        /// how a missing compiler came back as "Built 0 script(s)."
        /// </param>
        /// <param name="Purged">
        /// Canonical .ncs files removed because their source is now an include - see
        /// <see cref="BuildAllAsync"/>.
        /// </param>
        public readonly record struct BuildAllOutcome(bool Ran, int Compiled, int Failed, int Purged = 0);

        /// <summary>Compiles every entry-point script in the module.</summary>
        public Task<BuildAllOutcome> BuildAllAsync(CancellationToken cancellationToken = default) =>
            WithCompilerGateAsync(cancellationToken, () => BuildAllGatedAsync(cancellationToken));

        private async Task<BuildAllOutcome> BuildAllGatedAsync(CancellationToken cancellationToken)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || !IsAvailable)
            {
                _log.AppendLine("Cannot build scripts: no module open, or the compiler is missing.");
                return new BuildAllOutcome(Ran: false, 0, 0);
            }

            using var moduleWriteLock = ModuleWriteLock.Acquire(workspace.ModuleRoot);
            var compiled = 0;
            var failed = 0;
            var purged = 0;

            foreach (var resRef in workspace.EnumerateResRefs(ResourceType.Nss))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var source = workspace.GetResourcePath(ResourceType.Nss, resRef);
                if (!File.Exists(source))
                    continue;

                if (!ScriptStalenessScanner.IsEntryPoint(ScriptTextDocument.Load(source).Text))
                {
                    // The source is an include now, but the module may still carry bytecode from
                    // before it lost its entry point. CompileAsync's own include branch already
                    // handles this when a script is compiled directly; Build All skipped the source
                    // entirely instead, which is exactly why dmfi_dmw_inc.ncs survived every Build
                    // All since its source stopped declaring main() - the staleness scanner
                    // deliberately excludes includes, and the packer copies whatever ncs/ still has.
                    var obsoleteOutput = Path.Combine(workspace.ModuleRoot, "ncs", resRef + ".ncs");
                    if (File.Exists(obsoleteOutput))
                    {
                        File.Delete(obsoleteOutput);
                        _log.AppendLine($"Removed obsolete compiled output ncs/{resRef}.ncs (now an include).");
                        purged++;
                    }

                    continue;
                }

                if ((await CompileAsync(resRef, cancellationToken).ConfigureAwait(false)).Succeeded)
                    compiled++;
                else
                    failed++;
            }

            // The loop above iterates existing sources, so it can never touch an artifact whose
            // source was DELETED - the scanner flags those (SourceDeleted), and this is what
            // resolves them.
            foreach (var resRef in new ScriptStalenessScanner(
                         Path.Combine(workspace.ModuleRoot, "nss"),
                         Path.Combine(workspace.ModuleRoot, "ncs")).PurgeOrphanedArtifacts())
            {
                _log.AppendLine($"Removed orphaned compiled output ncs/{resRef}.ncs (source deleted).");
                purged++;
            }

            _log.AppendLine(purged == 0
                ? $"Build All Scripts: {compiled} compiled, {failed} failed."
                : $"Build All Scripts: {compiled} compiled, {failed} failed, {purged} obsolete include artifact(s) removed.");
            return new BuildAllOutcome(Ran: true, compiled, failed, purged);
        }

        /// <summary>Every script that transitively depends on an include.</summary>
        public IReadOnlyList<string> DependentsOf(string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<string>();

            return ScriptIncludeRebuildPlanner
                .Create(Path.Combine(workspace.ModuleRoot, "nss"), resRef)
                .Dependents;
        }

        /// <summary>Compiles/checks the supplied dependent scripts, preserving the usual include behaviour.</summary>
        public async Task<(int Compiled, int Failed)> BuildDependentsAsync(
            IEnumerable<string> resRefs,
            CancellationToken cancellationToken = default)
        {
            using var moduleWriteLock = _workspaceContext.Workspace is { } workspace
                ? ModuleWriteLock.Acquire(workspace.ModuleRoot)
                : null;
            var compiled = 0;
            var failed = 0;

            foreach (var resRef in resRefs.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var outcome = await CompileAsync(resRef, cancellationToken).ConfigureAwait(false);
                if (outcome.Succeeded)
                    compiled++;
                else
                    failed++;
            }

            _log.AppendLine($"Build Dependent Scripts: {compiled} compiled or checked, {failed} failed.");
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
                Path.Combine(workspace.ModuleRoot, "ncs"),
                CanResolveExternalInclude).Scan();
        }

        private ScriptIncludeGraph? IncludeGraph()
        {
            var workspace = _workspaceContext.Workspace;
            return workspace == null ? null : ScriptIncludeGraph.Build(Path.Combine(workspace.ModuleRoot, "nss"));
        }

        /// <summary>Discards a compile's temporary output. Never throws - a leaked .tmp is untidy,
        /// not harmful, and must never mask the real compile failure that got us here.</summary>
        private static void DeleteTempOutput(string temporaryOutput)
        {
            try
            {
                if (File.Exists(temporaryOutput))
                    File.Delete(temporaryOutput);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static bool IsEntryPoint(ModuleWorkspace workspace, string resRef)
        {
            var path = workspace.GetResourcePath(ResourceType.Nss, resRef);
            if (!File.Exists(path))
                return false;

            try
            {
                return ScriptStalenessScanner.IsEntryPoint(ScriptTextDocument.Load(path).Text);
            }
            catch (IOException)
            {
                return false;
            }
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

        private bool CanResolveExternalInclude(string resRef)
        {
            var staged = StageEngineHeader();
            if (staged != null &&
                File.Exists(Path.Combine(staged, resRef + ".nss")))
            {
                return true;
            }

            var resources = _workspaceContext.Workspace?.ResourceIndex;
            return resources != null &&
                   resources.ContainsBaseGameResource(
                       ResourceIdentity.FromFileName(resRef + ".nss"));
        }

        /// <summary>Copies the version-stamped header to a temp dir under the name the compiler expects.</summary>
        private string? StageEngineHeader()
        {
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
                var headerPath = Path.GetFullPath(header);
                var headerBytes = File.ReadAllBytes(headerPath);
                var identityBytes = System.Text.Encoding.UTF8.GetBytes(headerPath);
                using var identityHash = System.Security.Cryptography.IncrementalHash.CreateHash(
                    System.Security.Cryptography.HashAlgorithmName.SHA256);
                identityHash.AppendData(identityBytes);
                identityHash.AppendData(new byte[] { 0 });
                identityHash.AppendData(headerBytes);
                var stagingKey = Convert.ToHexString(identityHash.GetHashAndReset())
                    .ToLowerInvariant();
                var staging = Path.Combine(
                    Path.GetTempPath(),
                    "SWLOR.Toolset",
                    "nsscomp",
                    stagingKey);
                Directory.CreateDirectory(staging);
                var stagedHeader = Path.Combine(staging, "nwscript.nss");

                if (File.Exists(stagedHeader) &&
                    File.ReadAllBytes(stagedHeader).AsSpan().SequenceEqual(headerBytes))
                {
                    return staging;
                }

                // Never write in place: another toolset process may already be compiling against
                // this content-addressed directory. The rename publishes a complete header.
                var temporaryHeader = stagedHeader + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllBytes(temporaryHeader, headerBytes);
                    File.Move(temporaryHeader, stagedHeader, overwrite: true);
                }
                finally
                {
                    if (File.Exists(temporaryHeader))
                        File.Delete(temporaryHeader);
                }

                return staging;
            }
            catch (IOException ex)
            {
                _log.AppendLine($"Could not stage the NWScript header: {ex.Message}");
                return null;
            }
            catch (UnauthorizedAccessException ex)
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
