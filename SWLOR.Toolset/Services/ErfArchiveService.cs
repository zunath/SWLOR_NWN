using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using SWLOR.Toolset.Domain.Gff;
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
        string TypeName);

    public sealed record ErfDependency(string FileName, string Reason);

    public sealed record ErfPreparedImport(
        ErfArchiveAsset Asset,
        string ContentPath,
        string DestinationPath,
        ErfConflictKind Conflict,
        ErfConflictAction DefaultAction);

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
            @"^\s*#\s*include\s+""(?<resref>[A-Za-z0-9_]{1,16})""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

        private readonly WorkspaceContext _workspaceContext;
        private readonly OutputLogService _log;
        private readonly string? _erfToolOverride;
        private readonly string? _gffToolOverride;

        public ErfArchiveService(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            string? erfToolOverride = null,
            string? gffToolOverride = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _erfToolOverride = erfToolOverride;
            _gffToolOverride = gffToolOverride;
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

        public async Task<IReadOnlyList<ErfDependency>> FindImportDependenciesAsync(
            ErfArchiveSession session,
            IReadOnlyCollection<string> selectedFileNames,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(selectedFileNames);
            var tools = ResolveTools();
            var byFile = session.Assets.ToDictionary(asset => asset.FileName, StringComparer.OrdinalIgnoreCase);
            var byResRef = session.Assets
                .Where(asset => asset.IsSupported)
                .GroupBy(asset => asset.ResRef, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            return await FindDependenciesAsync(
                selectedFileNames,
                fileName => byFile.TryGetValue(fileName, out var asset) ? asset.Extension : null,
                fileName => byFile.TryGetValue(fileName, out var asset) ? asset.ResRef : null,
                resRef => byResRef.TryGetValue(resRef, out var matches)
                    ? matches.Select(asset => asset.FileName)
                    : Array.Empty<string>(),
                async fileName =>
                {
                    var asset = byFile[fileName];
                    if (GffExtensions.Contains(asset.Extension))
                    {
                        var jsonPath = await ConvertImportedGffAsync(session, asset, tools, cancellationToken)
                            .ConfigureAwait(false);
                        return FindJsonResRefs(jsonPath);
                    }

                    if (asset.Extension.Equals("nss", StringComparison.OrdinalIgnoreCase))
                        return FindScriptIncludes(session.ExtractedPath(asset));

                    return Array.Empty<string>();
                },
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<ErfDependency>> FindExportDependenciesAsync(
            IReadOnlyCollection<string> selectedFileNames,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(selectedFileNames);
            var assets = EnumerateModuleAssets();
            var byFile = assets.ToDictionary(asset => asset.FileName, StringComparer.OrdinalIgnoreCase);
            var byResRef = assets
                .GroupBy(asset => asset.ResRef, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            return await FindDependenciesAsync(
                selectedFileNames,
                fileName => byFile.TryGetValue(fileName, out var asset) ? asset.Extension : null,
                fileName => byFile.TryGetValue(fileName, out var asset) ? asset.ResRef : null,
                resRef => byResRef.TryGetValue(resRef, out var matches)
                    ? matches.Select(asset => asset.FileName)
                    : Array.Empty<string>(),
                fileName =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var asset = byFile[fileName];
                    IReadOnlyList<string> references = GffExtensions.Contains(asset.Extension)
                        ? FindJsonResRefs(asset.SourcePath)
                        : asset.Extension.Equals("nss", StringComparison.OrdinalIgnoreCase)
                            ? FindScriptIncludes(asset.SourcePath)
                            : Array.Empty<string>();
                    return Task.FromResult(references);
                },
                cancellationToken).ConfigureAwait(false);
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
                if (File.Exists(destination))
                {
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
                prepared.Add(new ErfPreparedImport(asset, contentPath, destination, conflict, defaultAction));
            }

            return prepared;
        }

        public Task<ErfImportResult> ImportAsync(
            IReadOnlyCollection<ErfImportChoice> choices,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(choices);
            ModuleMutationLock.ThrowIfModuleLocked();
            var workspace = RequireWorkspace();

            var normalizedChoices = NormalizeAreaRenameChoices(choices);
            var active = normalizedChoices
                .Where(choice => choice.Action is ErfConflictAction.Add
                    or ErfConflictAction.Replace
                    or ErfConflictAction.Rename)
                .ToList();
            var renameMap = ValidateImportPlan(active, workspace.ModuleRoot);
            var transactionRoot = CreateModuleTransactionDirectory(workspace.ModuleRoot);
            var stagedRoot = Path.Combine(transactionRoot, "staged");
            var rollbackRoot = Path.Combine(transactionRoot, "rollback");
            Directory.CreateDirectory(stagedRoot);
            Directory.CreateDirectory(rollbackRoot);

            var plan = new List<ImportPlanEntry>();
            string? recoveryManifestPath = null;
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
                        var document = JsonGffDocument.Load(choice.Prepared.ContentPath);
                        RewriteResRefs(document.Root, renameMap);
                        File.WriteAllBytes(stagedPath, document.ToBytes());
                        // Reparse the exact bytes that would be installed, not the pre-rewrite source.
                        _ = JsonGffDocument.Load(stagedPath);
                    }
                    else
                    {
                        File.Copy(choice.Prepared.ContentPath, stagedPath);
                    }

                    plan.Add(new ImportPlanEntry(
                        stagedPath,
                        destination,
                        asset.Extension,
                        targetResRef,
                        File.Exists(destination),
                        !string.Equals(targetResRef, asset.ResRef, StringComparison.OrdinalIgnoreCase)));
                }

                var duplicate = plan
                    .GroupBy(entry => entry.DestinationPath, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(group => group.Count() > 1);
                if (duplicate != null)
                    throw new InvalidOperationException(
                        $"More than one selected resource would be written to '{duplicate.Key}'.");

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
                        OriginalExisted = entry.ReplacesExisting
                    }).ToList());

                try
                {
                    foreach (var entry in plan)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Directory.CreateDirectory(Path.GetDirectoryName(entry.DestinationPath)!);

                        File.Move(entry.StagedPath, entry.DestinationPath, overwrite: true);
                    }
                }
                catch (Exception importException)
                {
                    try
                    {
                        RecoverManifest(workspace.ModuleRoot, recoveryManifestPath);
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

                File.Delete(recoveryManifestPath);
                recoveryManifestPath = null;

                var changed = plan
                    .Select(entry => (entry.Extension, entry.ResRef))
                    .ToList();
                foreach (var (extension, resRef) in changed)
                    RefreshWorkspace(extension, resRef);

                var skipped = normalizedChoices.Count - plan.Count;
                var result = new ErfImportResult(
                    plan.Count,
                    plan.Count(entry => entry.ReplacesExisting),
                    plan.Count(entry => entry.IsRename),
                    skipped,
                    backupDirectory,
                    changed);
                _log.AppendLine(
                    $"Imported {result.Imported} ERF resource(s); {result.Replaced} replaced, " +
                    $"{result.Renamed} renamed, {result.Skipped} skipped.");
                if (backupDirectory != null)
                    _log.AppendLine($"ERF replacement backups: {backupDirectory}");
                return Task.FromResult(result);
            }
            finally
            {
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

            var byFile = EnumerateModuleAssets()
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

        private static async Task<IReadOnlyList<ErfDependency>> FindDependenciesAsync(
            IReadOnlyCollection<string> selectedFileNames,
            Func<string, string?> extensionOf,
            Func<string, string?> resRefOf,
            Func<string, IEnumerable<string>> filesForResRef,
            Func<string, Task<IReadOnlyList<string>>> referencesOf,
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
                        var companion = filesForResRef(resRef).FirstOrDefault(candidate =>
                            string.Equals(extensionOf(candidate), companionExtension,
                                StringComparison.OrdinalIgnoreCase));
                        if (companion != null)
                            Add(companion, $"{fileName} area companion");
                    }
                }

                foreach (var referencedResRef in await referencesOf(fileName).ConfigureAwait(false))
                {
                    foreach (var referencedFile in filesForResRef(referencedResRef))
                        Add(referencedFile, $"Referenced by {fileName}");
                }
            }

            return reasons
                .Select(pair => new ErfDependency(pair.Key, pair.Value))
                .OrderBy(dependency => dependency.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IReadOnlyList<string> FindJsonResRefs(string path)
        {
            var document = JsonGffDocument.Load(path);
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectResRefs(document.Root, result);
            return result.ToList();
        }

        private static void CollectResRefs(JsonGffStruct current, ISet<string> result)
        {
            foreach (var (_, field) in current.Entries)
            {
                if (field.Type == GffFieldType.ResRef)
                {
                    var value = field.GetString();
                    if (IsValidResRef(value))
                        result.Add(value);
                }
                else if (field.Type == GffFieldType.Struct && field.Struct != null)
                {
                    CollectResRefs(field.Struct, result);
                }
                else if (field.Type == GffFieldType.List && field.Elements != null)
                {
                    foreach (var element in field.Elements)
                        CollectResRefs(element, result);
                }
            }
        }

        private static IReadOnlyList<string> FindScriptIncludes(string path)
        {
            var text = File.ReadAllText(path);
            return IncludePattern.Matches(text)
                .Select(match => match.Groups["resref"].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<ModuleArchiveAsset> EnumerateModuleAssetsCore(
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
                        DisplayType(extension));
                }
            }
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

                var renamed = choice.RenameResRef?.Trim() ?? string.Empty;
                if (!IsValidResRef(renamed))
                {
                    throw new InvalidOperationException(
                        $"The new resref for '{asset.FileName}' must be 1-16 ASCII letters, digits, or underscores.");
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

        private static IReadOnlyList<ErfImportChoice> NormalizeAreaRenameChoices(
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

            return normalized;
        }

        private static void RewriteResRefs(
            JsonGffStruct current,
            IReadOnlyDictionary<string, string> renameMap)
        {
            foreach (var (_, field) in current.Entries)
            {
                if (field.Type == GffFieldType.ResRef)
                {
                    var value = field.GetString();
                    var replacements = renameMap
                        .Where(pair => pair.Key.EndsWith("|" + value, StringComparison.OrdinalIgnoreCase))
                        .Select(pair => pair.Value)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (replacements.Count == 1)
                        field.SetString(replacements[0]);
                }
                else if (field.Type == GffFieldType.Struct && field.Struct != null)
                {
                    RewriteResRefs(field.Struct, renameMap);
                }
                else if (field.Type == GffFieldType.List && field.Elements != null)
                {
                    foreach (var element in field.Elements)
                        RewriteResRefs(element, renameMap);
                }
            }
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
                _workspaceContext.InvalidateTagIndex();
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

        private static IReadOnlyList<string> RecoverManifest(string moduleRoot, string manifestPath)
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

            var restored = new List<string>();
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

                if (entry.OriginalExisted)
                {
                    if (!File.Exists(rollback))
                    {
                        throw new FileNotFoundException(
                            $"The pre-import copy for '{destination}' is missing.", rollback);
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(rollback, destination, overwrite: true);
                }
                else if (File.Exists(destination))
                {
                    File.Delete(destination);
                }

                restored.Add(destination);
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

        private static bool IsSupportedExtension(string extension) =>
            GffExtensions.Contains(extension) || PlainExtensions.Contains(extension);

        private static bool IsValidResRef(string? value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 16)
                return false;

            return value.All(character => character is >= 'a' and <= 'z'
                or >= 'A' and <= 'Z'
                or >= '0' and <= '9'
                or '_');
        }

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
        }

        private sealed record ImportPlanEntry(
            string StagedPath,
            string DestinationPath,
            string Extension,
            string ResRef,
            bool ReplacesExisting,
            bool IsRename);
    }

    public sealed class ErfImportRecoveryException(string message, Exception innerException)
        : IOException(message, innerException);
}
