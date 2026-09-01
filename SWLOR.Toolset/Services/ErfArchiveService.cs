using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.Threading;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Services
{
    public enum ErfConflictKind
    {
        New,
        Identical,
        Different
    }

    public enum ErfConflictAction
    {
        Add,
        Skip,
        KeepExisting,
        Replace,
        Rename
    }

    public sealed record ErfArchiveAsset(
        string FileName,
        string ResRef,
        string Extension,
        long Size,
        bool IsSupported,
        string TypeName,
        string? UnsupportedReason);

    public sealed record ModuleArchiveAsset(
        string FileName,
        string ResRef,
        string Extension,
        string SourcePath,
        long Size,
        string TypeName,
        string? ResourceName = null);

    public sealed record ErfDependency(string FileName, string Reason);

    public sealed record ErfPreparedImport(
        ErfArchiveAsset Asset,
        string ContentPath,
        string DestinationPath,
        ErfConflictKind Conflict,
        ErfConflictAction DefaultAction,
        ErfDestinationFingerprint? DestinationFingerprint = null);

    public sealed record ErfDestinationFingerprint(long Length, string Sha256);

    public sealed record ErfImportChoice(
        ErfPreparedImport Prepared,
        ErfConflictAction Action,
        string? RenameResRef);

    public sealed record ErfImportResult(
        int Imported,
        int Replaced,
        int Renamed,
        int Skipped,
        string? BackupDirectory,
        IReadOnlyList<(string Extension, string ResRef)> ChangedResources);

    public sealed record ErfExportResult(int Exported, string DestinationPath);

    /// <summary>
    /// Owns a read-only extracted snapshot of one ERF while the import wizard is open.
    /// Extraction never targets the module, so merely browsing an archive cannot mutate it.
    /// </summary>
    public sealed class ErfArchiveSession : IDisposable
    {
        private bool _disposed;

        internal ErfArchiveSession(
            string sourcePath,
            string stagingDirectory,
            IReadOnlyList<ErfArchiveAsset> assets)
        {
            SourcePath = sourcePath;
            StagingDirectory = stagingDirectory;
            Assets = assets;
        }

        public string SourcePath { get; }
        public string StagingDirectory { get; }
        public IReadOnlyList<ErfArchiveAsset> Assets { get; }

        internal string ExtractedPath(ErfArchiveAsset asset) =>
            Path.Combine(StagingDirectory, asset.FileName);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ErfArchiveService.DeleteDirectoryBestEffort(StagingDirectory);
        }
    }

    /// <summary>
    /// Imports and exports Aurora ERF archives through the vendored neverwinter.nim tools. Every
    /// conversion is staged away from Module; import validates the complete plan before installing
    /// any file, and export validates a temporary archive before replacing the chosen destination.
    /// </summary>
    public sealed class ErfArchiveService
    {
        private const string ImportMarkerPrefix = ".swlor-toolset-erf-import-";
        private const string ImportMarkerSuffix = ".pending.json";

        private static readonly HashSet<string> GffExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            "are", "dlg", "fac", "gic", "git", "ifo", "itp", "jrl",
            "utc", "utd", "uti", "utm", "utp", "uts", "utt", "utw"
        };

        private static readonly HashSet<string> PlainExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            "nss", "ncs"
        };

        private static readonly Regex IncludePattern = new(
            $@"^\s*#\s*include\s+""(?<resref>[A-Za-z0-9_]{{1,{NwnResRef.MaxLength}}})""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
        private static readonly Regex CreateItemPattern = new(
            $@"\bCreateItemOnObject\s*\(\s*""(?<resref>[A-Za-z0-9_]{{1,{NwnResRef.MaxLength}}})""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex CreateObjectPattern = new(
            $@"\bCreateObject\s*\(\s*OBJECT_TYPE_(?<type>CREATURE|ITEM|PLACEABLE|STORE|WAYPOINT|DOOR|TRIGGER|SOUND)\s*,\s*""(?<resref>[A-Za-z0-9_]{{1,{NwnResRef.MaxLength}}})""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ExecuteScriptPattern = new(
            $@"\bExecuteScript\s*\(\s*""(?<resref>[A-Za-z0-9_]{{1,{NwnResRef.MaxLength}}})""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly WorkspaceContext _workspaceContext;
        private readonly OutputLogService _log;
        private readonly string? _erfToolOverride;
        private readonly string? _gffToolOverride;
        private readonly Func<Action, Task> _dispatchToUiThread;
        private readonly Func<string, CancellationToken, Task>? _reloadCustomContent;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _gffConversionLocks =
            new(StringComparer.OrdinalIgnoreCase);

        public ErfArchiveService(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            string? erfToolOverride = null,
            string? gffToolOverride = null,
            Func<Action, Task>? dispatchToUiThread = null,
            ModuleCustomContentService? moduleCustomContent = null,
            Func<string, CancellationToken, Task>? reloadCustomContent = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _erfToolOverride = erfToolOverride;
            _gffToolOverride = gffToolOverride;
            _dispatchToUiThread = dispatchToUiThread ?? DispatchToUiThreadAsync;
            _reloadCustomContent = reloadCustomContent ??
                (moduleCustomContent == null
                    ? null
                    : async (moduleRoot, cancellationToken) =>
                    {
                        var ifo = IfoDocument.Load(
                            Path.Combine(moduleRoot, "ifo", "module.ifo.json"));
                        await moduleCustomContent.ReloadAsync(
                                ifo.HakNames,
                                ifo.CustomTlk,
                                cancellationToken)
                            .ConfigureAwait(false);
                    });
        }

        public async Task<ErfArchiveSession> OpenArchiveAsync(
            string sourcePath,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
            sourcePath = Path.GetFullPath(sourcePath);
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("The selected ERF file does not exist.", sourcePath);
            if (!string.Equals(Path.GetExtension(sourcePath), ".erf", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Select an Aurora .erf archive.");
            if (new FileInfo(sourcePath).Length > uint.MaxValue)
                throw new InvalidDataException("The selected ERF is larger than Aurora's 4 GiB archive limit.");

            var tools = ResolveTools();
            var sourceDirectory = Path.GetDirectoryName(sourcePath)
                ?? throw new InvalidOperationException("The ERF file has no parent directory.");
            var sourceFileName = Path.GetFileName(sourcePath);

            var list = await RunToolAsync(
                tools.Erf,
                sourceDirectory,
                cancellationToken,
                "-f", sourceFileName, "-t").ConfigureAwait(false);

            var fileNames = list.StandardOutput
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();

            if (fileNames.Count == 0)
                throw new InvalidDataException("The selected ERF contains no resources.");
            if (fileNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != fileNames.Count)
                throw new InvalidDataException("The selected ERF contains duplicate resource names.");

            foreach (var fileName in fileNames)
                ValidateArchiveFileName(fileName);
            ValidateAreaTriplets(fileNames);

            var stagingDirectory = CreateTemporaryDirectory("import");
            try
            {
                // nwn_erf's Windows build is most reliable with simple relative paths. Keeping a
                // private copy in the extraction directory also guarantees the source cannot change
                // between the read-only scan and the final import.
                var snapshotFileName = $".archive-input-{Guid.NewGuid():N}.bin";
                var snapshotPath = Path.Combine(stagingDirectory, snapshotFileName);
                File.Copy(sourcePath, snapshotPath);

                await RunToolAsync(
                    tools.Erf,
                    stagingDirectory,
                    cancellationToken,
                    "-f", snapshotFileName, "-x").ConfigureAwait(false);

                var assets = new List<ErfArchiveAsset>(fileNames.Count);
                foreach (var fileName in fileNames)
                {
                    var extractedPath = Path.Combine(stagingDirectory, fileName);
                    if (!File.Exists(extractedPath))
                        throw new InvalidDataException($"ERF extraction did not produce '{fileName}'.");

                    var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
                    var resRef = Path.GetFileNameWithoutExtension(fileName);
                    var supported = IsSupportedExtension(extension);
                    assets.Add(new ErfArchiveAsset(
                        fileName,
                        resRef,
                        extension,
                        new FileInfo(extractedPath).Length,
                        supported,
                        DisplayType(extension),
                        supported ? null : $"The SWLOR module workspace does not store .{extension} resources."));
                }

                _log.AppendLine(
                    $"Scanned ERF '{Path.GetFileName(sourcePath)}': {assets.Count} resource(s), " +
                    $"{assets.Count(asset => asset.IsSupported)} importable.");
                return new ErfArchiveSession(sourcePath, stagingDirectory, assets);
            }
            catch
            {
                DeleteDirectoryBestEffort(stagingDirectory);
                throw;
            }
        }

        public IReadOnlyList<ModuleArchiveAsset> EnumerateModuleAssets()
        {
            var workspace = RequireWorkspace();
            return EnumerateModuleAssetsCore(workspace.ModuleRoot, CancellationToken.None)
                .OrderBy(asset => asset.TypeName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(asset => asset.ResRef, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async IAsyncEnumerable<IReadOnlyList<ModuleArchiveAsset>> EnumerateModuleAssetBatchesAsync(
            int batchSize = 64,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive.");

            var workspace = RequireWorkspace();
            using var enumerator = EnumerateModuleAssetsCore(
                    workspace.ModuleRoot,
                    cancellationToken)
                .GetEnumerator();

            while (true)
            {
                var batch = await Task.Run(
                        () =>
                        {
                            var discovered = new List<ModuleArchiveAsset>(batchSize);
                            while (discovered.Count < batchSize && enumerator.MoveNext())
                                discovered.Add(enumerator.Current);
                            return discovered;
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                if (batch.Count == 0)
                    yield break;

                yield return batch;
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> ReadModuleResourceNamesAsync(
            CancellationToken cancellationToken = default)
        {
            var catalog = _workspaceContext.Catalog;
            if (catalog == null)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            await catalog.BuildTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            return await Task.Run(
                    () => catalog.Entries
                        .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                        .ToDictionary(
                            entry => $"{entry.ResRef}.{entry.ResourceType.Extension()}",
                            entry => entry.Name!,
                            StringComparer.OrdinalIgnoreCase),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyDictionary<string, string>> ReadImportResourceNamesAsync(
            ErfArchiveSession session,
            IReadOnlyCollection<ErfArchiveAsset> assets,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(assets);

            var catalog = _workspaceContext.Catalog;
            if (catalog == null)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var tools = ResolveTools();
            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var asset in assets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ResourceTypeExtensions.TryFromExtension(asset.Extension, out var type) ||
                    (type != ResourceType.Area && !ModuleWorkspace.BlueprintTypes.Contains(type)))
                {
                    continue;
                }

                try
                {
                    var jsonPath = await ConvertImportedGffAsync(
                            session,
                            asset,
                            tools,
                            cancellationToken)
                        .ConfigureAwait(false);
                    var name = await Task.Run(
                            () => catalog.ReadDisplayName(type, File.ReadAllBytes(jsonPath)),
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(name))
                        names[asset.FileName] = name;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _log.AppendLine(
                        $"Could not read the display name for '{asset.FileName}': " +
                        ex.GetBaseException().Message);
                }
            }

            return names;
        }

        public async Task<IReadOnlyList<ErfDependency>> FindImportDependenciesAsync(
            ErfArchiveSession session,
            IReadOnlyCollection<string> selectedFileNames,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(selectedFileNames);
            var tools = ResolveTools();
            var byFile = session.Assets.ToDictionary(asset => asset.FileName, StringComparer.OrdinalIgnoreCase);
            var byResource = session.Assets
                .Where(asset => asset.IsSupported)
                .GroupBy(
                    asset => ArchiveKey(asset.Extension, asset.ResRef),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            return await FindDependenciesAsync(
                selectedFileNames,
                fileName => byFile.TryGetValue(fileName, out var asset) ? asset.Extension : null,
                fileName => byFile.TryGetValue(fileName, out var asset) ? asset.ResRef : null,
                (extension, resRef) => byResource.TryGetValue(
                    ArchiveKey(extension, resRef),
                    out var matches)
                    ? matches.Select(asset => asset.FileName)
                    : Array.Empty<string>(),
                async fileName =>
                {
                    var asset = byFile[fileName];
                    if (GffExtensions.Contains(asset.Extension))
                    {
                        var jsonPath = await ConvertImportedGffAsync(session, asset, tools, cancellationToken)
                            .ConfigureAwait(false);
                        return FindJsonResourceReferences(jsonPath, asset.Extension);
                    }

                    if (asset.Extension.Equals("nss", StringComparison.OrdinalIgnoreCase))
                    {
                        return FindScriptResourceReferences(session.ExtractedPath(asset));
                    }

                    return Array.Empty<ErfResourceReference>();
                },
                cancellationToken).ConfigureAwait(false);
        }

        public Task ValidateExportSelectionAsync(
            IReadOnlyCollection<ModuleArchiveAsset> selectedAssets,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(selectedAssets);
            return Task.Run(
                    () =>
                    {
                        foreach (var asset in selectedAssets.DistinctBy(
                                     candidate => candidate.FileName,
                                     StringComparer.OrdinalIgnoreCase))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (GffExtensions.Contains(asset.Extension))
                                _ = JsonGffDocument.Load(asset.SourcePath);
                            else
                                _ = File.ReadAllBytes(asset.SourcePath);
                        }
                    },
                    cancellationToken);
        }

        public async Task<IReadOnlyList<ErfPreparedImport>> PrepareImportAsync(
            ErfArchiveSession session,
            IReadOnlyCollection<string> selectedFileNames,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(selectedFileNames);
            var workspace = RequireWorkspace();
            var tools = ResolveTools();
            var selected = new HashSet<string>(selectedFileNames, StringComparer.OrdinalIgnoreCase);
            var prepared = new List<ErfPreparedImport>();
            ValidateAreaTriplets(selected);

            foreach (var asset in session.Assets.Where(asset => selected.Contains(asset.FileName)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!asset.IsSupported)
                    continue;

                var contentPath = GffExtensions.Contains(asset.Extension)
                    ? await ConvertImportedGffAsync(session, asset, tools, cancellationToken).ConfigureAwait(false)
                    : session.ExtractedPath(asset);
                var destination = ModuleDestination(workspace.ModuleRoot, asset.Extension, asset.ResRef);
                var conflict = ErfConflictKind.New;
                ErfDestinationFingerprint? destinationFingerprint = null;
                if (File.Exists(destination))
                {
                    destinationFingerprint = Fingerprint(destination);
                    conflict = FilesEqual(contentPath, destination)
                        ? ErfConflictKind.Identical
                        : ErfConflictKind.Different;
                }

                var defaultAction = conflict switch
                {
                    ErfConflictKind.New => ErfConflictAction.Add,
                    ErfConflictKind.Identical => ErfConflictAction.Skip,
                    _ => ErfConflictAction.KeepExisting
                };
                prepared.Add(new ErfPreparedImport(
                    asset,
                    contentPath,
                    destination,
                    conflict,
                    defaultAction,
                    destinationFingerprint));
            }

            return prepared;
        }

        public async Task<ErfImportResult> ImportAsync(
            IReadOnlyCollection<ErfImportChoice> choices,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(choices);
            ModuleMutationLock.ThrowIfModuleLocked();
            var moduleRoot = RequireWorkspace().ModuleRoot;
            var snapshot = choices.ToList();
            var result = await Task.Run(
                    () => Import(snapshot, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);

            if (_reloadCustomContent != null && result.ChangedResources.Any(resource =>
                    resource.Extension.Equals("ifo", StringComparison.OrdinalIgnoreCase) &&
                    resource.ResRef.Equals("module", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    // The transaction is committed at this point. Finish synchronizing the live
                    // HAK/TLK stack even if the caller cancels while the post-commit refresh runs;
                    // reporting cancellation here would imply the already-installed import rolled
                    // back when it did not.
                    await _reloadCustomContent(moduleRoot, CancellationToken.None)
                        .ConfigureAwait(false);
                    _log.AppendLine("Reloaded module custom content after importing module.ifo.");
                }
                catch (Exception ex)
                {
                    _log.AppendLine(
                        "Imported module.ifo, but its custom content could not be reloaded: " +
                        ex.GetBaseException().Message);
                }
            }

            await _dispatchToUiThread(() =>
            {
                foreach (var (extension, resRef) in result.ChangedResources)
                    RefreshWorkspace(extension, resRef);
            }).ConfigureAwait(false);

            return result;
        }

        private ErfImportResult Import(
            IReadOnlyCollection<ErfImportChoice> choices,
            CancellationToken cancellationToken)
        {
            var workspace = RequireWorkspace();
            using var moduleWriteLock = ModuleWriteLock.Acquire(workspace.ModuleRoot);

            var normalizedChoices = NormalizeImportChoices(choices);
            var active = normalizedChoices
                .Where(choice => choice.Action is ErfConflictAction.Add
                    or ErfConflictAction.Replace
                    or ErfConflictAction.Rename)
                .ToList();
            var renameMap = ValidateImportPlan(active, workspace.ModuleRoot);
            ValidateAreaTriplets(active
                .Select(choice => choice.Prepared.Asset.FileName)
                .ToList());
            ValidateScriptCompanions(active, renameMap, workspace.ModuleRoot);
            ValidateCompiledScriptGenerations(active, renameMap);
            var transactionRoot = CreateModuleTransactionDirectory(workspace.ModuleRoot);
            var stagedRoot = Path.Combine(transactionRoot, "staged");
            var rollbackRoot = Path.Combine(transactionRoot, "rollback");
            Directory.CreateDirectory(stagedRoot);
            Directory.CreateDirectory(rollbackRoot);

            var plan = new List<ImportPlanEntry>();
            byte[]? moduleIfoBaseline = null;
            string? recoveryManifestPath = null;
            ModuleIfoUpdateLock? ifoUpdateLock = null;
            try
            {
                foreach (var choice in active)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var asset = choice.Prepared.Asset;
                    var targetResRef = renameMap.TryGetValue(
                        ArchiveKey(asset.Extension, asset.ResRef), out var renamed)
                        ? renamed
                        : asset.ResRef;
                    var destination = ModuleDestination(workspace.ModuleRoot, asset.Extension, targetResRef);
                    var stagedPath = Path.Combine(
                        stagedRoot,
                        asset.Extension,
                        Path.GetFileName(destination));
                    Directory.CreateDirectory(Path.GetDirectoryName(stagedPath)!);

                    if (GffExtensions.Contains(asset.Extension))
                    {
                        using (EditScope.EnterConstruction())
                        {
                            var document = JsonGffDocument.Load(choice.Prepared.ContentPath);
                            RewriteOwnResRef(
                                document.Root,
                                asset.Extension,
                                asset.ResRef,
                                targetResRef);
                            RewriteTypedReferences(
                                document.Root,
                                renameMap,
                                asset.Extension);

                            File.WriteAllBytes(stagedPath, document.ToBytes());
                        }
                        // Reparse the exact bytes that would be installed, not the pre-rewrite source.
                        _ = JsonGffDocument.Load(stagedPath);
                    }
                    else if (asset.Extension.Equals("nss", StringComparison.OrdinalIgnoreCase))
                    {
                        var script = ScriptTextDocument.Load(choice.Prepared.ContentPath);
                        File.WriteAllBytes(
                            stagedPath,
                            script.ToBytes(RewriteScriptReferences(script.Text, renameMap)));
                    }
                    else
                    {
                        File.Copy(choice.Prepared.ContentPath, stagedPath);
                    }

                    var expectedDestination = choice.Action == ErfConflictAction.Rename
                        ? null
                        : choice.Prepared.DestinationFingerprint;
                    if (expectedDestination == null &&
                        choice.Action != ErfConflictAction.Rename &&
                        choice.Prepared.Conflict != ErfConflictKind.New &&
                        File.Exists(destination))
                    {
                        // Compatibility for callers that construct a prepared choice directly.
                        // The wizard always supplies the selection-time fingerprint above.
                        expectedDestination = Fingerprint(destination);
                    }

                    plan.Add(new ImportPlanEntry(
                        stagedPath,
                        destination,
                        asset.Extension,
                        targetResRef,
                        ReplacesExisting: expectedDestination != null,
                        IsRename: !string.Equals(
                            targetResRef,
                            asset.ResRef,
                            StringComparison.OrdinalIgnoreCase),
                        IsMetadata: false,
                        ExpectedDestination: expectedDestination));
                }

                ApplyEconomyRestrictions(plan, workspace);

                var importedAreas = plan
                    .Where(entry => entry.Extension.Equals("are", StringComparison.OrdinalIgnoreCase))
                    .Select(entry => entry.ResRef)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (importedAreas.Count > 0)
                {
                    ifoUpdateLock = ModuleIfoUpdateLock.Acquire(workspace.ModuleRoot);
                    var ifoPath = ModuleDestination(workspace.ModuleRoot, "ifo", "module");
                    var selectedIfoIndex = plan.FindIndex(entry =>
                        entry.DestinationPath.Equals(ifoPath, StringComparison.OrdinalIgnoreCase));
                    IfoDocument ifo;
                    if (selectedIfoIndex >= 0)
                    {
                        ifo = IfoDocument.Load(plan[selectedIfoIndex].StagedPath);
                    }
                    else
                    {
                        if (!File.Exists(ifoPath))
                            throw new FileNotFoundException(
                                "module.ifo.json was not found; imported areas cannot be registered.",
                                ifoPath);

                        moduleIfoBaseline = File.ReadAllBytes(ifoPath);
                        ifo = IfoDocument.Parse(moduleIfoBaseline);
                    }

                    var ifoChanged = false;
                    using (var ifoSession = new DocumentSession(ifoPath, ifo.Document))
                    using (var transaction = ifoSession.Begin("Register imported areas"))
                    {
                        foreach (var areaResRef in importedAreas)
                            ifoChanged |= AreaTemplateFactory.AddAreaToModule(ifo, areaResRef);
                        transaction.Commit();
                    }

                    if (ifoChanged)
                    {
                        if (selectedIfoIndex >= 0)
                        {
                            var selectedIfo = plan[selectedIfoIndex];
                            File.WriteAllBytes(selectedIfo.StagedPath, ifo.ToBytes());
                            _ = IfoDocument.Load(selectedIfo.StagedPath);
                        }
                        else
                        {
                            var stagedIfo = Path.Combine(stagedRoot, "ifo", "module.ifo.json");
                            Directory.CreateDirectory(Path.GetDirectoryName(stagedIfo)!);
                            File.WriteAllBytes(stagedIfo, ifo.ToBytes());
                            _ = IfoDocument.Load(stagedIfo);
                            plan.Add(new ImportPlanEntry(
                                stagedIfo,
                                ifoPath,
                                "ifo",
                                "module",
                                ReplacesExisting: true,
                                IsRename: false,
                                IsMetadata: true,
                                ExpectedDestination: Fingerprint(moduleIfoBaseline!)));
                        }
                    }
                }

                var duplicate = plan
                    .GroupBy(entry => entry.DestinationPath, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(group => group.Count() > 1);
                if (duplicate != null)
                    throw new InvalidOperationException(
                        $"More than one selected resource would be written to '{duplicate.Key}'.");

                foreach (var entry in plan)
                    ValidateDestination(entry);

                var backupDirectory = CreateBackupDirectory(plan.Any(entry => entry.ReplacesExisting));
                foreach (var entry in plan.Where(entry => entry.ReplacesExisting))
                {
                    var rollbackPath = RollbackPath(
                        rollbackRoot, workspace.ModuleRoot, entry.DestinationPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(rollbackPath)!);
                    File.Copy(entry.DestinationPath, rollbackPath);

                    if (backupDirectory != null)
                    {
                        var backupPath = RollbackPath(
                            backupDirectory, workspace.ModuleRoot, entry.DestinationPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                        File.Copy(entry.DestinationPath, backupPath);
                    }
                }

                recoveryManifestPath = WriteRecoveryManifest(
                    workspace.ModuleRoot,
                    transactionRoot,
                    plan.Select(entry => new ImportRecoveryEntry
                    {
                        DestinationPath = entry.DestinationPath,
                        RollbackPath = RollbackPath(
                            rollbackRoot, workspace.ModuleRoot, entry.DestinationPath),
                        OriginalExisted = entry.ReplacesExisting,
                        InstalledContent = Fingerprint(entry.StagedPath)
                    }).ToList());

                var installedDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    foreach (var entry in plan)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Directory.CreateDirectory(Path.GetDirectoryName(entry.DestinationPath)!);
                        ValidateDestination(entry);
                        File.Move(
                            entry.StagedPath,
                            entry.DestinationPath,
                            overwrite: entry.ReplacesExisting);
                        installedDestinations.Add(entry.DestinationPath);
                    }

                    File.Delete(recoveryManifestPath);
                    recoveryManifestPath = null;
                }
                catch (Exception importException)
                {
                    try
                    {
                        RecoverManifest(
                            workspace.ModuleRoot,
                            recoveryManifestPath!,
                            installedDestinations);
                        recoveryManifestPath = null;
                    }
                    catch (Exception recoveryException)
                    {
                        throw new ErfImportRecoveryException(
                            $"ERF import failed and automatic rollback could not complete. " +
                            $"Recovery evidence was preserved at '{recoveryManifestPath}'.",
                            new AggregateException(importException, recoveryException));
                    }

                    throw;
                }

                var changed = plan
                    .Where(entry => !entry.IsMetadata)
                    .Select(entry => (entry.Extension, entry.ResRef))
                    .ToList();
                var importedPlan = plan.Where(entry => !entry.IsMetadata).ToList();
                var skipped = normalizedChoices.Count - importedPlan.Count;
                var result = new ErfImportResult(
                    importedPlan.Count,
                    importedPlan.Count(entry => entry.ReplacesExisting),
                    importedPlan.Count(entry => entry.IsRename),
                    skipped,
                    backupDirectory,
                    changed);
                _log.AppendLine(
                    $"Imported {result.Imported} ERF resource(s); {result.Replaced} replaced, " +
                    $"{result.Renamed} renamed, {result.Skipped} skipped.");
                if (backupDirectory != null)
                    _log.AppendLine($"ERF replacement backups: {backupDirectory}");
                return result;
            }
            finally
            {
                ifoUpdateLock?.Dispose();

                // A surviving marker means rollback did not finish. Its transaction directory holds
                // the only guaranteed copy of a pre-import generation and must remain for startup
                // recovery instead of being treated as ordinary temp debris.
                if (recoveryManifestPath == null || !File.Exists(recoveryManifestPath))
                    DeleteDirectoryBestEffort(transactionRoot);
            }
        }

        /// <summary>
        /// Restores the pre-import generation after a process or machine interruption landed during
        /// the multi-file commit. Called before WorkspaceContext exposes Module to any editor.
        /// </summary>
        public static IReadOnlyList<string> RecoverInterruptedImports(string moduleRoot)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
            moduleRoot = Path.GetFullPath(moduleRoot);
            if (!Directory.Exists(moduleRoot))
                return Array.Empty<string>();

            using var moduleWriteLock = ModuleWriteLock.Acquire(moduleRoot);
            var recovered = new List<string>();
            foreach (var manifestPath in Directory.EnumerateFiles(
                         moduleRoot,
                         ImportMarkerPrefix + "*" + ImportMarkerSuffix,
                         SearchOption.TopDirectoryOnly))
            {
                try
                {
                    recovered.AddRange(RecoverManifest(moduleRoot, manifestPath));
                }
                catch (Exception ex) when (ex is not ErfImportRecoveryException)
                {
                    throw new ErfImportRecoveryException(
                        $"Could not recover interrupted ERF import '{manifestPath}'. " +
                        "The module was not opened because its resources may be from mixed generations.",
                        ex);
                }
            }

            return recovered;
        }

        public async Task<ErfExportResult> ExportAsync(
            IReadOnlyCollection<string> selectedFileNames,
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(selectedFileNames);
            ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
            var workspace = RequireWorkspace();
            var tools = ResolveTools();
            destinationPath = Path.GetFullPath(destinationPath);
            if (!string.Equals(Path.GetExtension(destinationPath), ".erf", StringComparison.OrdinalIgnoreCase))
                destinationPath += ".erf";

            // Enumeration, source copying, and archive validation are one read snapshot. The same
            // cross-process lease used by imports prevents another toolset or CLI process from
            // replacing one member of an area or script pair while that snapshot is being built.
            using var moduleWriteLock = ModuleWriteLock.Acquire(workspace.ModuleRoot);
            var byFile = EnumerateModuleAssetsCore(workspace.ModuleRoot, cancellationToken)
                .OrderBy(asset => asset.TypeName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(asset => asset.ResRef, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(asset => asset.FileName, StringComparer.OrdinalIgnoreCase);
            var selected = selectedFileNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(fileName => byFile.TryGetValue(fileName, out var asset)
                    ? asset
                    : throw new InvalidOperationException(
                        $"The selected module resource '{fileName}' no longer exists."))
                .ToList();
            if (selected.Count == 0)
                throw new InvalidOperationException("Select at least one module resource to export.");

            var stagingDirectory = CreateTemporaryDirectory("export");
            var resourcesDirectory = Path.Combine(stagingDirectory, "resources");
            var jsonDirectory = Path.Combine(stagingDirectory, "json");
            Directory.CreateDirectory(resourcesDirectory);
            Directory.CreateDirectory(jsonDirectory);
            try
            {
                foreach (var asset in selected)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (GffExtensions.Contains(asset.Extension))
                    {
                        _ = JsonGffDocument.Load(asset.SourcePath);
                        var stagedJson = Path.Combine(jsonDirectory, asset.FileName + ".json");
                        File.Copy(asset.SourcePath, stagedJson);
                        await RunToolAsync(
                            tools.Gff,
                            stagingDirectory,
                            cancellationToken,
                            "-l", "json",
                            "-i", Path.Combine("json", asset.FileName + ".json"),
                            "-o", Path.Combine("resources", asset.FileName),
                            "-k", "gff").ConfigureAwait(false);

                        var converted = Path.Combine(resourcesDirectory, asset.FileName);
                        if (!File.Exists(converted))
                        {
                            throw new InvalidOperationException(
                                $"GFF conversion did not produce '{asset.FileName}'.");
                        }
                    }
                    else
                    {
                        File.Copy(asset.SourcePath, Path.Combine(resourcesDirectory, asset.FileName));
                    }
                }

                var archiveName = "archive.erf";
                await RunToolAsync(
                    tools.Erf,
                    stagingDirectory,
                    cancellationToken,
                    "-e", "ERF",
                    "-f", archiveName,
                    "-c", "resources").ConfigureAwait(false);

                var validation = await RunToolAsync(
                    tools.Erf,
                    stagingDirectory,
                    cancellationToken,
                    "-f", archiveName, "-t").ConfigureAwait(false);
                var listed = validation.StandardOutput
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var missing = selected.FirstOrDefault(asset => !listed.Contains(asset.FileName));
                if (missing != null)
                    throw new InvalidDataException(
                        $"The temporary ERF did not contain expected resource '{missing.FileName}'.");

                var destinationDirectory = Path.GetDirectoryName(destinationPath)
                    ?? throw new InvalidOperationException("The export path has no parent directory.");
                Directory.CreateDirectory(destinationDirectory);
                var adjacentTemporary = Path.Combine(
                    destinationDirectory,
                    $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");
                try
                {
                    File.Copy(Path.Combine(stagingDirectory, archiveName), adjacentTemporary);
                    await RunToolAsync(
                        tools.Erf,
                        destinationDirectory,
                        cancellationToken,
                        "-f", Path.GetFileName(adjacentTemporary), "-t").ConfigureAwait(false);
                    File.Move(adjacentTemporary, destinationPath, overwrite: true);
                }
                finally
                {
                    if (File.Exists(adjacentTemporary))
                        File.Delete(adjacentTemporary);
                }

                _log.AppendLine(
                    $"Exported {selected.Count} resource(s) to ERF '{destinationPath}'.");
                return new ErfExportResult(selected.Count, destinationPath);
            }
            finally
            {
                DeleteDirectoryBestEffort(stagingDirectory);
            }
        }

        private async Task<string> ConvertImportedGffAsync(
            ErfArchiveSession session,
            ErfArchiveAsset asset,
            ToolPaths tools,
            CancellationToken cancellationToken)
        {
            var convertedDirectory = Path.Combine(session.StagingDirectory, "converted");
            Directory.CreateDirectory(convertedDirectory);
            var relativeOutput = Path.Combine("converted", asset.FileName + ".json");
            var output = Path.Combine(session.StagingDirectory, relativeOutput);
            if (File.Exists(output))
                return output;

            var conversionLock = _gffConversionLocks.GetOrAdd(
                output,
                _ => new SemaphoreSlim(1, 1));
            await conversionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (File.Exists(output))
                    return output;

                await RunToolAsync(
                    tools.Gff,
                    session.StagingDirectory,
                    cancellationToken,
                    "-i", asset.FileName,
                    "-o", relativeOutput,
                    "-p").ConfigureAwait(false);
                _ = JsonGffDocument.Load(output);
                return output;
            }
            finally
            {
                conversionLock.Release();
            }
        }

        private static async Task<IReadOnlyList<ErfDependency>> FindDependenciesAsync(
            IReadOnlyCollection<string> selectedFileNames,
            Func<string, string?> extensionOf,
            Func<string, string?> resRefOf,
            Func<string, string, IEnumerable<string>> filesForResource,
            Func<string, Task<IReadOnlyList<ErfResourceReference>>> referencesOf,
            CancellationToken cancellationToken)
        {
            var selected = new HashSet<string>(selectedFileNames, StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>(selected);
            var reasons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            void Add(string fileName, string reason)
            {
                if (!selected.Add(fileName))
                    return;

                reasons[fileName] = reason;
                queue.Enqueue(fileName);
            }

            while (queue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileName = queue.Dequeue();
                var extension = extensionOf(fileName);
                var resRef = resRefOf(fileName);
                if (extension == null || resRef == null)
                    continue;

                if (extension.Equals("are", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var companionExtension in new[] { "git", "gic" })
                    {
                        var companion = filesForResource(companionExtension, resRef).FirstOrDefault();
                        if (companion == null)
                        {
                            throw new InvalidDataException(
                                $"Area '{resRef}' is incomplete; the ERF is missing " +
                                $".{companionExtension}.");
                        }

                        Add(companion, $"{fileName} area companion");
                    }
                }
                else if (extension.Equals("nss", StringComparison.OrdinalIgnoreCase))
                {
                    var compiledCompanion = filesForResource("ncs", resRef).FirstOrDefault();
                    if (compiledCompanion != null)
                        Add(compiledCompanion, $"{fileName} compiled script companion");
                }
                else if (extension.Equals("ncs", StringComparison.OrdinalIgnoreCase))
                {
                    var sourceCompanion = filesForResource("nss", resRef).FirstOrDefault();
                    if (sourceCompanion != null)
                        Add(sourceCompanion, $"{fileName} script source companion");
                }

                foreach (var reference in await referencesOf(fileName).ConfigureAwait(false))
                {
                    var referencedFiles = filesForResource(
                            reference.Extension,
                            reference.ResRef)
                        .ToList();
                    if (referencedFiles.Count == 0 &&
                        reference.AllowCompiledFallback &&
                        reference.Extension.Equals("nss", StringComparison.OrdinalIgnoreCase))
                    {
                        referencedFiles = filesForResource("ncs", reference.ResRef).ToList();
                    }

                    foreach (var referencedFile in referencedFiles)
                        Add(referencedFile, $"Referenced by {fileName}");
                }
            }

            return reasons
                .Select(pair => new ErfDependency(pair.Key, pair.Value))
                .OrderBy(dependency => dependency.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IReadOnlyList<ErfResourceReference> FindJsonResourceReferences(
            string path,
            string resourceExtension)
        {
            var document = JsonGffDocument.Load(path);
            var result = new Dictionary<string, ErfResourceReference>(StringComparer.OrdinalIgnoreCase);
            CollectResourceReferences(document.Root, result, resourceExtension);
            return result.Values.ToList();
        }

        private static void CollectResourceReferences(
            JsonGffStruct current,
            IDictionary<string, ErfResourceReference> result,
            string resourceExtension,
            string? containingList = null)
        {
            foreach (var (name, field) in current.Entries)
            {
                if (field.Type == GffFieldType.ResRef)
                {
                    var value = field.GetString();
                    var extension = ReferencedExtension(name, containingList, resourceExtension);
                    if (extension != null && IsValidResRef(value))
                    {
                        result[ArchiveKey(extension, value)] =
                            new ErfResourceReference(
                                extension,
                                value,
                                AllowCompiledFallback: extension.Equals(
                                    "nss",
                                    StringComparison.OrdinalIgnoreCase));
                    }
                }
                else if (field.Type == GffFieldType.Struct && field.Struct != null)
                {
                    CollectResourceReferences(field.Struct, result, resourceExtension, containingList);
                }
                else if (field.Type == GffFieldType.List && field.Elements != null)
                {
                    foreach (var element in field.Elements)
                        CollectResourceReferences(element, result, resourceExtension, name);
                }
            }
        }

        private static IReadOnlyList<ErfResourceReference> FindScriptResourceReferences(string path)
        {
            var text = File.ReadAllText(path);
            var result = new Dictionary<string, ErfResourceReference>(
                StringComparer.OrdinalIgnoreCase);

            void Add(string extension, string resRef, bool allowCompiledFallback = false)
            {
                var key = ArchiveKey(extension, resRef);
                if (!result.TryGetValue(key, out var existing) ||
                    allowCompiledFallback && !existing.AllowCompiledFallback)
                {
                    result[key] = new ErfResourceReference(
                        extension,
                        resRef,
                        allowCompiledFallback);
                }
            }

            foreach (Match match in IncludePattern.Matches(text))
                Add("nss", match.Groups["resref"].Value);

            foreach (Match match in CreateItemPattern.Matches(text))
                Add("uti", match.Groups["resref"].Value);

            foreach (Match match in CreateObjectPattern.Matches(text))
            {
                var extension = ScriptObjectExtension(match.Groups["type"].Value);
                if (extension != null)
                    Add(extension, match.Groups["resref"].Value);
            }

            foreach (Match match in ExecuteScriptPattern.Matches(text))
                Add(
                    "nss",
                    match.Groups["resref"].Value,
                    allowCompiledFallback: true);

            return result.Values.ToList();
        }

        private IEnumerable<ModuleArchiveAsset> EnumerateModuleAssetsCore(
            string moduleRoot,
            CancellationToken cancellationToken)
        {
            var extensions = GffExtensions
                .Concat(PlainExtensions)
                .OrderBy(DisplayType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(extension => extension, StringComparer.OrdinalIgnoreCase);

            foreach (var extension in extensions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var directory = Path.Combine(moduleRoot, extension);
                if (!Directory.Exists(directory))
                    continue;

                var isGff = GffExtensions.Contains(extension);
                var suffix = isGff ? $".{extension}.json" : $".{extension}";
                var pattern = isGff ? $"*.{extension}.json" : $"*.{extension}";
                foreach (var path in Directory.EnumerateFiles(
                             directory,
                             pattern,
                             SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var fileName = Path.GetFileName(path);
                    if (!fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var resRef = fileName[..^suffix.Length];
                    if (!IsValidResRef(resRef))
                        continue;

                    yield return new ModuleArchiveAsset(
                        $"{resRef}.{extension}",
                        resRef,
                        extension,
                        path,
                        new FileInfo(path).Length,
                        DisplayType(extension),
                        KnownModuleResourceName(extension, resRef));
                }
            }
        }

        private string? KnownModuleResourceName(string extension, string resRef)
        {
            if (!ResourceTypeExtensions.TryFromExtension(extension, out var type))
                return null;

            return _workspaceContext.Catalog?.TryGetEntry(type, resRef, out var entry) == true
                ? entry.Name
                : null;
        }

        private static Dictionary<string, string> ValidateImportPlan(
            IReadOnlyCollection<ErfImportChoice> active,
            string moduleRoot)
        {
            var renameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var choice in active)
            {
                var asset = choice.Prepared.Asset;
                if (choice.Action == ErfConflictAction.Add &&
                    choice.Prepared.Conflict != ErfConflictKind.New)
                {
                    throw new InvalidOperationException(
                        $"'{asset.FileName}' is not a new resource and cannot use Add.");
                }

                if (choice.Action == ErfConflictAction.Replace &&
                    choice.Prepared.Conflict == ErfConflictKind.New)
                {
                    throw new InvalidOperationException(
                        $"'{asset.FileName}' has no existing resource to replace.");
                }

                if (choice.Action != ErfConflictAction.Rename)
                    continue;

                if (!CanRenameResource(asset.Extension, asset.ResRef))
                {
                    throw new InvalidOperationException(
                        $"'{asset.FileName}' is a fixed-name module resource and cannot be renamed.");
                }

                var renamed = choice.RenameResRef?.Trim() ?? string.Empty;
                if (!IsValidResRef(renamed))
                {
                    throw new InvalidOperationException(
                        $"The new resref for '{asset.FileName}' must be 1-{NwnResRef.MaxLength} " +
                        "ASCII letters, digits, or underscores.");
                }

                renameMap.Add(ArchiveKey(asset.Extension, asset.ResRef), renamed);
            }

            // ARE/GIT/GIC form one logical resource. If any member is renamed, every selected
            // companion follows it even when its own source happened to be "New" rather than a
            // conflict. Leaving one member under the old resref produces an area the engine cannot
            // open.
            var areaExtensions = new HashSet<string>(
                new[] { "are", "git", "gic" },
                StringComparer.OrdinalIgnoreCase);
            foreach (var areaGroup in active
                         .Where(choice => areaExtensions.Contains(choice.Prepared.Asset.Extension))
                         .GroupBy(choice => choice.Prepared.Asset.ResRef, StringComparer.OrdinalIgnoreCase))
            {
                var renamedValues = areaGroup
                    .Select(choice => renameMap.TryGetValue(
                        ArchiveKey(choice.Prepared.Asset.Extension, choice.Prepared.Asset.ResRef),
                        out var value) ? value : null)
                    .Where(value => value != null)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (renamedValues.Count > 1)
                {
                    throw new InvalidOperationException(
                        $"Area companions for '{areaGroup.Key}' must use the same renamed resref.");
                }

                if (renamedValues.Count == 1)
                {
                    foreach (var choice in areaGroup)
                    {
                        renameMap[ArchiveKey(
                            choice.Prepared.Asset.Extension,
                            choice.Prepared.Asset.ResRef)] = renamedValues[0]!;
                    }
                }
            }

            foreach (var choice in active)
            {
                var asset = choice.Prepared.Asset;
                var renamed = renameMap.TryGetValue(
                    ArchiveKey(asset.Extension, asset.ResRef), out var mapped)
                    ? mapped
                    : asset.ResRef;
                var target = ModuleDestination(moduleRoot, asset.Extension, renamed);
                var targetExists = File.Exists(target);
                if (choice.Action == ErfConflictAction.Add && targetExists)
                {
                    throw new InvalidOperationException(
                        $"Cannot add '{asset.FileName}': its destination now exists. Re-scan conflicts.");
                }

                if (choice.Action == ErfConflictAction.Replace && !targetExists)
                {
                    throw new InvalidOperationException(
                        $"Cannot replace '{asset.FileName}': its destination no longer exists. Re-scan conflicts.");
                }

                if (choice.Action == ErfConflictAction.Rename && targetExists)
                {
                    throw new InvalidOperationException(
                        $"Cannot rename '{asset.FileName}' to '{renamed}': that module resource already exists.");
                }
            }

            return renameMap;
        }

        private static IReadOnlyList<ErfImportChoice> NormalizeImportChoices(
            IReadOnlyCollection<ErfImportChoice> choices)
        {
            var normalized = choices.ToList();
            var areaExtensions = new HashSet<string>(
                new[] { "are", "git", "gic" },
                StringComparer.OrdinalIgnoreCase);
            foreach (var group in normalized
                         .Where(choice => areaExtensions.Contains(choice.Prepared.Asset.Extension))
                         .GroupBy(choice => choice.Prepared.Asset.ResRef, StringComparer.OrdinalIgnoreCase))
            {
                var renamedValues = group
                    .Where(choice => choice.Action == ErfConflictAction.Rename)
                    .Select(choice => choice.RenameResRef?.Trim())
                    .Where(value => !string.IsNullOrEmpty(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (renamedValues.Count == 0)
                    continue;
                if (renamedValues.Count > 1)
                {
                    throw new InvalidOperationException(
                        $"Area companions for '{group.Key}' must use the same renamed resref.");
                }

                var renamed = renamedValues[0]!;
                foreach (var companion in group)
                {
                    var index = normalized.IndexOf(companion);
                    normalized[index] = companion with
                    {
                        Action = ErfConflictAction.Rename,
                        RenameResRef = renamed
                    };
                }
            }

            foreach (var group in normalized
                         .Where(choice =>
                             choice.Prepared.Asset.Extension.Equals(
                                 "nss",
                                 StringComparison.OrdinalIgnoreCase) ||
                             choice.Prepared.Asset.Extension.Equals(
                                 "ncs",
                                 StringComparison.OrdinalIgnoreCase))
                         .GroupBy(
                             choice => choice.Prepared.Asset.ResRef,
                             StringComparer.OrdinalIgnoreCase))
            {
                var source = group.FirstOrDefault(choice =>
                    choice.Prepared.Asset.Extension.Equals(
                        "nss",
                        StringComparison.OrdinalIgnoreCase));
                var compiled = group.FirstOrDefault(choice =>
                    choice.Prepared.Asset.Extension.Equals(
                        "ncs",
                        StringComparison.OrdinalIgnoreCase));
                if (source == null ||
                    compiled == null ||
                    source.Action is not (
                        ErfConflictAction.Add or
                        ErfConflictAction.Replace or
                        ErfConflictAction.Rename))
                {
                    continue;
                }

                var compiledAction = source.Action == ErfConflictAction.Rename
                    ? ErfConflictAction.Rename
                    : compiled.Prepared.Conflict == ErfConflictKind.New
                        ? ErfConflictAction.Add
                        : ErfConflictAction.Replace;
                var index = normalized.IndexOf(compiled);
                normalized[index] = compiled with
                {
                    Action = compiledAction,
                    RenameResRef = source.Action == ErfConflictAction.Rename
                        ? source.RenameResRef
                        : null
                };
            }

            return normalized;
        }

        /// <summary>
        /// Refuses executable script source without the bytecode NWN will actually run. Importing
        /// source alone would leave Add/Rename without an executable and Replace paired with stale
        /// destination bytecode. Include-only sources intentionally remain valid without NCS files.
        /// </summary>
        private static void ValidateScriptCompanions(
            IReadOnlyCollection<ErfImportChoice> choices,
            IReadOnlyDictionary<string, string> renameMap,
            string moduleRoot)
        {
            var sourceResRefs = choices
                .Where(choice => choice.Prepared.Asset.Extension.Equals(
                    "nss",
                    StringComparison.OrdinalIgnoreCase))
                .Select(choice => choice.Prepared.Asset.ResRef)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var compiledResRefs = choices
                .Where(choice => choice.Prepared.Asset.Extension.Equals(
                    "ncs",
                    StringComparison.OrdinalIgnoreCase))
                .Select(choice => choice.Prepared.Asset.ResRef)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var choice in choices.Where(choice =>
                         choice.Prepared.Asset.Extension.Equals(
                             "ncs",
                             StringComparison.OrdinalIgnoreCase) &&
                         !sourceResRefs.Contains(choice.Prepared.Asset.ResRef)))
            {
                var targetResRef = renameMap.TryGetValue(
                    ArchiveKey("ncs", choice.Prepared.Asset.ResRef),
                    out var renamed)
                    ? renamed
                    : choice.Prepared.Asset.ResRef;
                var existingSourcePath = ModuleDestination(moduleRoot, "nss", targetResRef);
                if (File.Exists(existingSourcePath))
                {
                    throw new InvalidOperationException(
                        $"Cannot import compiled script '{choice.Prepared.Asset.FileName}' without " +
                        $"its matching source while '{targetResRef}.nss' already exists in the module. " +
                        "Include the NSS from the same build or remove the stale module source.");
                }
            }

            foreach (var choice in choices.Where(choice =>
                         choice.Prepared.Asset.Extension.Equals(
                             "nss",
                             StringComparison.OrdinalIgnoreCase)))
            {
                var script = ScriptTextDocument.Load(choice.Prepared.ContentPath);
                var isEntryPoint = ScriptStalenessScanner.IsEntryPoint(script.Text);
                var hasCompiledCompanion = compiledResRefs.Contains(
                    choice.Prepared.Asset.ResRef);
                if (isEntryPoint && !hasCompiledCompanion)
                {
                    throw new InvalidOperationException(
                        $"Cannot import entry-point script '{choice.Prepared.Asset.FileName}' without " +
                        $"its compiled companion '{choice.Prepared.Asset.ResRef}.ncs'. Include the " +
                        "matching NCS from the same build, or import this source after compiling it.");
                }

                if (isEntryPoint || hasCompiledCompanion)
                {
                    if (!isEntryPoint)
                    {
                        throw new InvalidOperationException(
                            $"Cannot import include-only script '{choice.Prepared.Asset.FileName}' " +
                            "with a compiled NCS companion. Remove the stale bytecode from the " +
                            "archive before importing.");
                    }

                    continue;
                }

                var targetResRef = renameMap.TryGetValue(
                    ArchiveKey("nss", choice.Prepared.Asset.ResRef),
                    out var renamed)
                    ? renamed
                    : choice.Prepared.Asset.ResRef;
                var staleCompiledPath = ModuleDestination(moduleRoot, "ncs", targetResRef);
                if (File.Exists(staleCompiledPath))
                {
                    throw new InvalidOperationException(
                        $"Cannot import include-only script '{choice.Prepared.Asset.FileName}' while " +
                        $"the stale compiled artifact '{targetResRef}.ncs' exists. Remove the NCS or " +
                        "import a matching source/bytecode pair.");
                }
            }
        }

        /// <summary>
        /// Refuses an import whose source rewrite would detach a selected compiled companion from
        /// the source generation that produced it. The importer cannot patch NCS bytecode, and
        /// installing it under either the original or renamed resref would leave NWN executing the
        /// old literals while the editor displays the new ones.
        /// </summary>
        private static void ValidateCompiledScriptGenerations(
            IReadOnlyCollection<ErfImportChoice> choices,
            IReadOnlyDictionary<string, string> renameMap)
        {
            var compiledResRefs = choices
                .Where(choice => choice.Prepared.Asset.Extension.Equals(
                    "ncs",
                    StringComparison.OrdinalIgnoreCase))
                .Select(choice => choice.Prepared.Asset.ResRef)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var choice in choices.Where(choice =>
                         choice.Prepared.Asset.Extension.Equals(
                             "nss",
                             StringComparison.OrdinalIgnoreCase) &&
                         compiledResRefs.Contains(choice.Prepared.Asset.ResRef)))
            {
                var script = ScriptTextDocument.Load(choice.Prepared.ContentPath);
                var rewritten = RewriteScriptReferences(script.Text, renameMap);
                if (string.Equals(script.Text, rewritten, StringComparison.Ordinal))
                    continue;

                throw new InvalidOperationException(
                    $"Cannot import '{choice.Prepared.Asset.FileName}' with its compiled companion " +
                    "because renamed resource references change the script source but not its NCS " +
                    "bytecode. Import without those renames, or recompile the rewritten script before importing.");
            }
        }

        private static void RewriteOwnResRef(
            JsonGffStruct root,
            string extension,
            string sourceResRef,
            string targetResRef)
        {
            if (string.Equals(sourceResRef, targetResRef, StringComparison.OrdinalIgnoreCase))
                return;

            var identityField = extension.ToLowerInvariant() switch
            {
                "are" or "utm" => "ResRef",
                "utc" or "utd" or "uti" or "utp" or "uts" or "utt" or "utw" =>
                    "TemplateResRef",
                _ => null
            };
            if (identityField == null || root.GetOrNull(identityField) is not { } field ||
                field.Type != GffFieldType.ResRef ||
                !string.Equals(field.GetString(), sourceResRef, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Only the resource's own identity is rewritten here. A bare GFF ResRef carries no
            // target type, so matching by text alone can turn a creature reference into an item
            // rename when both types happen to share a resref.
            field.SetString(targetResRef);
        }

        private static void RewriteTypedReferences(
            JsonGffStruct root,
            IReadOnlyDictionary<string, string> renameMap,
            string resourceExtension,
            string? containingList = null)
        {
            foreach (var (name, field) in root.Entries)
            {
                if (field.Type == GffFieldType.ResRef)
                {
                    var referencedExtension =
                        ReferencedExtension(name, containingList, resourceExtension);
                    var current = field.GetString();
                    if (referencedExtension != null &&
                        TryGetReferenceRename(
                            referencedExtension,
                            current,
                            renameMap,
                            out var replacement))
                    {
                        field.SetString(replacement);
                    }
                }

                if (field.Type == GffFieldType.Struct && field.Struct != null)
                {
                    RewriteTypedReferences(
                        field.Struct,
                        renameMap,
                        resourceExtension,
                        containingList);
                }
                else if (field.Type == GffFieldType.List && field.Elements != null)
                {
                    foreach (var element in field.Elements)
                        RewriteTypedReferences(element, renameMap, resourceExtension, name);
                }
            }
        }

        private static string? ReferencedExtension(
            string fieldName,
            string? containingList,
            string resourceExtension)
        {
            if (fieldName.StartsWith("Script", StringComparison.OrdinalIgnoreCase) ||
                fieldName.StartsWith("On", StringComparison.OrdinalIgnoreCase))
            {
                return "nss";
            }

            if (resourceExtension.Equals("dlg", StringComparison.OrdinalIgnoreCase) &&
                fieldName is "Active" or "EndConversation" or "EndConverAbort")
            {
                return "nss";
            }

            if (fieldName.Equals("Conversation", StringComparison.OrdinalIgnoreCase))
                return "dlg";

            if (fieldName.Equals("InventoryRes", StringComparison.OrdinalIgnoreCase))
                return "uti";

            if (fieldName.Equals("EquippedRes", StringComparison.OrdinalIgnoreCase))
                return "uti";

            if (fieldName.Equals("Area_Name", StringComparison.OrdinalIgnoreCase))
                return "are";

            if (fieldName.Equals("TemplateResRef", StringComparison.OrdinalIgnoreCase))
            {
                return containingList?.ToLowerInvariant() switch
                {
                    "creature list" => "utc",
                    "door list" => "utd",
                    "encounter list" => "ute",
                    "equip_itemlist" => "uti",
                    "itemlist" => "uti",
                    "list" => "uti",
                    "placeable list" => "utp",
                    "soundlist" => "uts",
                    "triggerlist" => "utt",
                    "waypointlist" => "utw",
                    _ => null
                };
            }

            if (fieldName.Equals("ResRef", StringComparison.OrdinalIgnoreCase) &&
                containingList?.Equals("StoreList", StringComparison.OrdinalIgnoreCase) == true)
            {
                return "utm";
            }

            return null;
        }

        private static bool TryGetReferenceRename(
            string extension,
            string resRef,
            IReadOnlyDictionary<string, string> renameMap,
            out string replacement)
        {
            if (renameMap.TryGetValue(ArchiveKey(extension, resRef), out replacement!))
                return true;

            return extension.Equals("nss", StringComparison.OrdinalIgnoreCase) &&
                   renameMap.TryGetValue(ArchiveKey("ncs", resRef), out replacement!);
        }

        internal static bool CanRenameResource(string extension, string resRef)
        {
            return !(
                (extension.Equals("ifo", StringComparison.OrdinalIgnoreCase) &&
                 resRef.Equals("module", StringComparison.OrdinalIgnoreCase)) ||
                (extension.Equals("fac", StringComparison.OrdinalIgnoreCase) &&
                 resRef.Equals("repute", StringComparison.OrdinalIgnoreCase)) ||
                (extension.Equals("jrl", StringComparison.OrdinalIgnoreCase) &&
                 resRef.Equals("module", StringComparison.OrdinalIgnoreCase)));
        }

        private static string? GameSourceRoot(string moduleRoot)
        {
            var repositoryRoot = Path.GetDirectoryName(Path.GetFullPath(moduleRoot));
            if (repositoryRoot == null)
                return null;

            var candidate = Path.Combine(repositoryRoot, "SWLOR.Game.Server");
            return Directory.Exists(candidate) ? candidate : null;
        }

        private static void ApplyEconomyRestrictions(
            IReadOnlyList<ImportPlanEntry> plan,
            ModuleWorkspace workspace)
        {
            var importedItems = plan
                .Where(entry => entry.Extension.Equals("uti", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (importedItems.Count == 0)
                return;

            var sourceOverrides = new List<(ResourceType Type, string ResRef, string SourcePath)>();
            foreach (var entry in plan)
            {
                if (!ResourceTypeExtensions.TryFromExtension(entry.Extension, out var type) ||
                    type is not (ResourceType.Utm or ResourceType.Utc or ResourceType.Utp))
                {
                    continue;
                }

                sourceOverrides.Add((type, entry.ResRef, entry.StagedPath));
            }

            var itemSources = ItemObtainabilityIndex.Build(
                workspace,
                GameSourceRoot(workspace.ModuleRoot),
                sourceOverrides,
                plan.Where(entry => entry.Extension.Equals(
                        "nss",
                        StringComparison.OrdinalIgnoreCase))
                    .Select(entry => entry.StagedPath));
            foreach (var item in importedItems)
            {
                using (EditScope.EnterConstruction())
                {
                    var document = JsonGffDocument.Load(item.StagedPath);
                    var variables = new VarTable(document.Root);
                    if (variables.GetInt(BlueprintTemplateFactory.NoEconomyVariable) != 1 &&
                        !itemSources.IsObtainable(item.ResRef))
                    {
                        variables.SetInt(BlueprintTemplateFactory.NoEconomyVariable, 1);
                        File.WriteAllBytes(item.StagedPath, document.ToBytes());
                    }
                }

                _ = JsonGffDocument.Load(item.StagedPath);
            }
        }

        private static string RewriteScriptReferences(
            string source,
            IReadOnlyDictionary<string, string> renameMap)
        {
            source = ReplaceScriptResRef(source, IncludePattern, "nss", renameMap);
            source = ReplaceScriptResRef(source, CreateItemPattern, "uti", renameMap);
            source = ReplaceScriptResRef(
                source,
                ExecuteScriptPattern,
                "nss",
                renameMap,
                allowCompiledFallback: true);
            return CreateObjectPattern.Replace(source, match =>
            {
                var extension = ScriptObjectExtension(match.Groups["type"].Value) ?? string.Empty;
                return ReplaceScriptResRefMatch(match, extension, renameMap);
            });
        }

        private static string? ScriptObjectExtension(string objectType) =>
            objectType switch
            {
                "CREATURE" => "utc",
                "ITEM" => "uti",
                "PLACEABLE" => "utp",
                "STORE" => "utm",
                "WAYPOINT" => "utw",
                "DOOR" => "utd",
                "TRIGGER" => "utt",
                "SOUND" => "uts",
                _ => null
            };

        private static string ReplaceScriptResRef(
            string source,
            Regex pattern,
            string extension,
            IReadOnlyDictionary<string, string> renameMap,
            bool allowCompiledFallback = false)
        {
            return pattern.Replace(
                source,
                match => ReplaceScriptResRefMatch(
                    match,
                    extension,
                    renameMap,
                    allowCompiledFallback));
        }

        private static string ReplaceScriptResRefMatch(
            Match match,
            string extension,
            IReadOnlyDictionary<string, string> renameMap,
            bool allowCompiledFallback = false)
        {
            var group = match.Groups["resref"];
            var hasReplacement = allowCompiledFallback
                ? TryGetReferenceRename(extension, group.Value, renameMap, out var replacement)
                : renameMap.TryGetValue(
                    ArchiveKey(extension, group.Value),
                    out replacement!);
            if (!hasReplacement)
            {
                return match.Value;
            }

            var relativeStart = group.Index - match.Index;
            return match.Value[..relativeStart] + replacement +
                   match.Value[(relativeStart + group.Length)..];
        }

        private static bool FilesEqual(string left, string right)
        {
            var leftInfo = new FileInfo(left);
            var rightInfo = new FileInfo(right);
            if (leftInfo.Length != rightInfo.Length)
                return false;

            const int bufferSize = 81920;
            using var leftStream = File.OpenRead(left);
            using var rightStream = File.OpenRead(right);
            var leftBuffer = new byte[bufferSize];
            var rightBuffer = new byte[bufferSize];
            while (true)
            {
                var leftRead = leftStream.Read(leftBuffer);
                var rightRead = rightStream.Read(rightBuffer);
                if (leftRead != rightRead)
                    return false;
                if (leftRead == 0)
                    return true;
                if (!leftBuffer.AsSpan(0, leftRead).SequenceEqual(rightBuffer.AsSpan(0, rightRead)))
                    return false;
            }
        }

        private static ErfDestinationFingerprint Fingerprint(string path)
        {
            using var stream = File.OpenRead(path);
            return new ErfDestinationFingerprint(
                stream.Length,
                Convert.ToHexString(SHA256.HashData(stream)));
        }

        private static ErfDestinationFingerprint Fingerprint(byte[] content) =>
            new(content.LongLength, Convert.ToHexString(SHA256.HashData(content)));

        private static void ValidateDestination(ImportPlanEntry entry)
        {
            if (entry.ExpectedDestination == null)
            {
                if (File.Exists(entry.DestinationPath))
                {
                    throw new IOException(
                        $"'{entry.DestinationPath}' was created after the ERF import was prepared. " +
                        "Nothing was overwritten; review the module and try again.");
                }

                return;
            }

            if (!File.Exists(entry.DestinationPath) ||
                Fingerprint(entry.DestinationPath) != entry.ExpectedDestination)
            {
                throw new IOException(
                    $"'{entry.DestinationPath}' changed after the ERF import was prepared. " +
                    "Nothing was overwritten; review the newer file and try again.");
            }
        }

        private static string ModuleDestination(string moduleRoot, string extension, string resRef)
        {
            var fileName = GffExtensions.Contains(extension)
                ? $"{resRef}.{extension}.json"
                : $"{resRef}.{extension}";
            return Path.Combine(moduleRoot, extension, fileName);
        }

        private static string RollbackPath(string root, string moduleRoot, string destination) =>
            Path.Combine(root, Path.GetRelativePath(moduleRoot, destination));

        private static string ArchiveKey(string extension, string resRef) =>
            $"{extension}|{resRef}";

        private void RefreshWorkspace(string extension, string resRef)
        {
            if (ResourceTypeExtensions.TryFromExtension(extension, out var type))
            {
                _workspaceContext.RefreshCatalogEntry(type, resRef);
                return;
            }

            if (extension.Equals("git", StringComparison.OrdinalIgnoreCase))
            {
                _workspaceContext.InvalidateGitIndexes();
                _workspaceContext.InvalidateScriptUsages();
            }
            else if (extension.Equals("itp", StringComparison.OrdinalIgnoreCase))
            {
                _workspaceContext.InvalidatePaletteChoices(resRef);
            }
        }

        private ModuleWorkspace RequireWorkspace() =>
            _workspaceContext.Workspace
            ?? throw new InvalidOperationException("Open a module before using ERF archives.");

        private ToolPaths ResolveTools()
        {
            if (_erfToolOverride != null && _gffToolOverride != null)
                return RequireTools(_erfToolOverride, _gffToolOverride);

            var workspace = RequireWorkspace();
            var toolsDirectory = FindToolsDirectory(workspace.ModuleRoot)
                                 ?? FindToolsDirectory(AppContext.BaseDirectory)
                                 ?? throw new DirectoryNotFoundException(
                                     "Could not find the repository's tools/SWLOR.CLI directory.");
            return RequireTools(
                _erfToolOverride ?? Path.Combine(toolsDirectory, "nwn_erf.exe"),
                _gffToolOverride ?? Path.Combine(toolsDirectory, "nwn_gff.exe"));
        }

        private static string? FindToolsDirectory(string start)
        {
            var current = new DirectoryInfo(start);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "tools", "SWLOR.CLI");
                if (Directory.Exists(candidate))
                    return candidate;
                current = current.Parent;
            }

            return null;
        }

        private static ToolPaths RequireTools(string erf, string gff)
        {
            if (!File.Exists(erf))
                throw new FileNotFoundException("The vendored nwn_erf.exe tool was not found.", erf);
            if (!File.Exists(gff))
                throw new FileNotFoundException("The vendored nwn_gff.exe tool was not found.", gff);
            return new ToolPaths(erf, gff);
        }

        private static async Task<ToolResult> RunToolAsync(
            string executable,
            string workingDirectory,
            CancellationToken cancellationToken,
            params string[] arguments)
        {
            var startInfo = new ProcessStartInfo(executable)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // The original cancellation/start failure remains the useful exception.
                }
                throw;
            }
            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);
            if (process.ExitCode != 0)
            {
                var detail = string.Join(
                    Environment.NewLine,
                    new[] { output.Trim(), error.Trim() }.Where(value => value.Length > 0));
                throw new InvalidOperationException(
                    $"{Path.GetFileName(executable)} failed with exit code {process.ExitCode}." +
                    (detail.Length == 0 ? string.Empty : Environment.NewLine + detail));
            }

            return new ToolResult(output, error);
        }

        private static string CreateTemporaryDirectory(string purpose)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "SWLOR.Toolset",
                "ErfArchives",
                $"{purpose}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        private static string CreateModuleTransactionDirectory(string moduleRoot)
        {
            var parent = Directory.GetParent(moduleRoot)?.FullName
                ?? throw new InvalidOperationException("The module root has no parent directory.");
            var path = Path.Combine(parent, $"{ImportMarkerPrefix}{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        private static string WriteRecoveryManifest(
            string moduleRoot,
            string transactionRoot,
            List<ImportRecoveryEntry> entries)
        {
            var manifest = new ImportRecoveryManifest
            {
                TransactionRoot = transactionRoot,
                Entries = entries
            };
            var id = Path.GetFileName(transactionRoot)[ImportMarkerPrefix.Length..];
            var path = Path.Combine(moduleRoot, ImportMarkerPrefix + id + ImportMarkerSuffix);
            var temporaryPath = path + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(manifest));
                File.Move(temporaryPath, path);
                return path;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static IReadOnlyList<string> RecoverManifest(
            string moduleRoot,
            string manifestPath,
            IReadOnlySet<string>? destinationsToRecover = null)
        {
            moduleRoot = Path.GetFullPath(moduleRoot);
            manifestPath = Path.GetFullPath(manifestPath);
            RequirePathUnder(moduleRoot, manifestPath, "recovery manifest");

            var manifest = JsonSerializer.Deserialize<ImportRecoveryManifest>(
                File.ReadAllText(manifestPath));
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.TransactionRoot) ||
                manifest.Entries == null)
            {
                throw new InvalidDataException("The ERF import recovery manifest is incomplete.");
            }

            var transactionRoot = Path.GetFullPath(manifest.TransactionRoot);
            var moduleParent = Directory.GetParent(moduleRoot)?.FullName
                ?? throw new InvalidDataException("The module root has no parent directory.");
            RequirePathUnder(moduleParent, transactionRoot, "transaction directory");
            if (!string.Equals(
                    Directory.GetParent(transactionRoot)?.FullName,
                    moduleParent,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "The recovery manifest names a transaction outside the module's parent directory.");
            }
            if (!Path.GetFileName(transactionRoot).StartsWith(
                    ImportMarkerPrefix, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The recovery manifest names an invalid transaction directory.");
            }

            var recoverable = new List<(
                ImportRecoveryEntry Entry,
                string Destination,
                string Rollback,
                bool DestinationMatchesOriginal)>();
            foreach (var entry in manifest.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.DestinationPath) ||
                    string.IsNullOrWhiteSpace(entry.RollbackPath))
                {
                    throw new InvalidDataException("The recovery manifest contains an incomplete entry.");
                }

                var destination = Path.GetFullPath(entry.DestinationPath);
                var rollback = Path.GetFullPath(entry.RollbackPath);
                RequirePathUnder(moduleRoot, destination, "module destination");
                RequirePathUnder(transactionRoot, rollback, "rollback file");

                if (destinationsToRecover != null &&
                    !destinationsToRecover.Contains(destination))
                {
                    continue;
                }

                if (entry.OriginalExisted)
                {
                    if (!File.Exists(rollback))
                    {
                        throw new FileNotFoundException(
                            $"The pre-import copy for '{destination}' is missing.", rollback);
                    }
                }

                var destinationMatchesOriginal =
                    entry.OriginalExisted &&
                    File.Exists(destination) &&
                    FilesEqual(destination, rollback);
                var destinationMatchesInstalled =
                    entry.InstalledContent != null &&
                    File.Exists(destination) &&
                    Fingerprint(destination) == entry.InstalledContent;
                if (File.Exists(destination) &&
                    !destinationMatchesOriginal &&
                    !destinationMatchesInstalled)
                {
                    throw new IOException(
                        $"'{destination}' changed after the interrupted ERF import. " +
                        "Recovery was refused so the newer file is preserved.");
                }

                recoverable.Add((entry, destination, rollback, destinationMatchesOriginal));
            }

            var restored = new List<string>();
            foreach (var item in recoverable)
            {
                if (item.Entry.OriginalExisted)
                {
                    if (!item.DestinationMatchesOriginal)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(item.Destination)!);
                        File.Copy(item.Rollback, item.Destination, overwrite: true);
                    }
                }
                else if (File.Exists(item.Destination))
                {
                    File.Delete(item.Destination);
                }

                restored.Add(item.Destination);
            }

            File.Delete(manifestPath);
            DeleteDirectoryBestEffort(transactionRoot);
            return restored;
        }

        private static void RequirePathUnder(string root, string candidate, string description)
        {
            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root))
                                 + Path.DirectorySeparatorChar;
            var normalizedCandidate = Path.GetFullPath(candidate);
            if (!normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"The {description} escapes its expected directory.");
        }

        private static string? CreateBackupDirectory(bool needed)
        {
            if (!needed)
                return null;

            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SWLOR.Toolset",
                "Backups",
                "ErfImports",
                $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        internal static void DeleteDirectoryBestEffort(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
                // Staging cleanup must not mask the archive operation's real result.
            }
        }

        private static void ValidateArchiveFileName(string fileName)
        {
            if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal) ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidDataException($"ERF entry '{fileName}' has an unsafe file name.");
            }

            var extension = Path.GetExtension(fileName);
            var resRef = Path.GetFileNameWithoutExtension(fileName);
            if (extension.Length <= 1 || !IsValidResRef(resRef))
                throw new InvalidDataException($"ERF entry '{fileName}' is not a valid Aurora resource name.");
            var extensionText = extension[1..];
            if (extensionText.Length > 4 ||
                extensionText.Any(character => character is not (>= 'a' and <= 'z'
                    or >= 'A' and <= 'Z'
                    or >= '0' and <= '9')))
            {
                throw new InvalidDataException($"ERF entry '{fileName}' has an invalid resource type.");
            }
        }

        private static void ValidateAreaTriplets(IReadOnlyCollection<string> fileNames)
        {
            var areaExtensions = new HashSet<string>(
                new[] { "are", "git", "gic" },
                StringComparer.OrdinalIgnoreCase);
            var groups = fileNames
                .Select(fileName => (
                    FileName: fileName,
                    ResRef: Path.GetFileNameWithoutExtension(fileName),
                    Extension: Path.GetExtension(fileName).TrimStart('.')))
                .Where(item => areaExtensions.Contains(item.Extension))
                .GroupBy(item => item.ResRef, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                var extensions = group.Select(item => item.Extension).ToList();
                var present = extensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var missing = areaExtensions.Where(extension => !present.Contains(extension)).ToList();
                if (missing.Count > 0)
                {
                    throw new InvalidDataException(
                        $"Area '{group.Key}' is incomplete; the ERF is missing " +
                        string.Join(", ", missing.Select(extension => "." + extension)) + ".");
                }

                var duplicates = extensions
                    .GroupBy(extension => extension, StringComparer.OrdinalIgnoreCase)
                    .Where(extension => extension.Count() > 1)
                    .Select(extension => "." + extension.Key)
                    .ToList();
                if (duplicates.Count > 0)
                {
                    throw new InvalidDataException(
                        $"Area '{group.Key}' contains duplicate " +
                        string.Join(", ", duplicates) + " resources.");
                }
            }
        }

        private static async Task DispatchToUiThreadAsync(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                action();
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(action);
        }

        private static bool IsSupportedExtension(string extension) =>
            GffExtensions.Contains(extension) || PlainExtensions.Contains(extension);

        private static bool IsValidResRef(string? value) => NwnResRef.IsValid(value);

        private static string DisplayType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                "are" => "Area",
                "git" => "Area instances",
                "gic" => "Area comments",
                "utc" => "Creature",
                "uti" => "Item",
                "utp" => "Placeable",
                "utd" => "Door",
                "utm" => "Merchant",
                "utt" => "Trigger",
                "uts" => "Sound",
                "utw" => "Waypoint",
                "dlg" => "Dialog",
                "nss" => "Script source",
                "ncs" => "Compiled script",
                "ifo" => "Module properties",
                "fac" => "Factions",
                "itp" => "Palette",
                "jrl" => "Journal",
                _ => $".{extension} resource"
            };
        }

        private sealed record ToolPaths(string Erf, string Gff);
        private sealed record ToolResult(string StandardOutput, string StandardError);
        private sealed class ImportRecoveryManifest
        {
            public string TransactionRoot { get; set; } = string.Empty;
            public List<ImportRecoveryEntry> Entries { get; set; } = new();
        }

        private sealed class ImportRecoveryEntry
        {
            public string DestinationPath { get; set; } = string.Empty;
            public string RollbackPath { get; set; } = string.Empty;
            public bool OriginalExisted { get; set; }
            public ErfDestinationFingerprint? InstalledContent { get; set; }
        }

        private sealed record ImportPlanEntry(
            string StagedPath,
            string DestinationPath,
            string Extension,
            string ResRef,
            bool ReplacesExisting,
            bool IsRename,
            bool IsMetadata,
            ErfDestinationFingerprint? ExpectedDestination);

        private sealed record ErfResourceReference(
            string Extension,
            string ResRef,
            bool AllowCompiledFallback = false);
    }

    public sealed class ErfImportRecoveryException(string message, Exception innerException)
        : IOException(message, innerException);
}
