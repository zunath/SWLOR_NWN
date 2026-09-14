using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Services
{
    public sealed record BlueprintSaveOutcome(
        bool Saved,
        bool Renamed,
        string OldResRef,
        string NewResRef,
        string OldPath,
        string NewPath,
        int UpdatedInstances,
        IReadOnlyList<string> UpdatedAreas)
    {
        public static BlueprintSaveOutcome Failed(
            string resRef,
            string path) =>
            new(false, false, resRef, resRef, path, path, 0, Array.Empty<string>());
    }

    /// <summary>
    /// One rename-on-save transaction for every placeable blueprint type. It renames the file,
    /// carries custom-category membership, updates placed-instance references without rebuilding
    /// their builder-authored overrides, and rolls the entire set back together if any member
    /// cannot be written.
    /// </summary>
    public sealed class BlueprintSaveCoordinator
    {
        private static readonly string[] ReferenceFolders =
        {
            "utc", "uti", "utp", "utd", "utm", "utt", "uts", "utw", "dlg", "nss"
        };

        private readonly OutputLogService _log;
        private readonly CategoryService? _categories;
        private readonly Func<string, bool>? _hasUnsavedAreaInstances;
        private readonly Action<string>? _reloadOpenAreaInstances;
        private readonly Func<string, IReadOnlyList<string>>? _findUnsavedReferences;

        public BlueprintSaveCoordinator(
            OutputLogService log,
            CategoryService? categories = null,
            Func<string, bool>? hasUnsavedAreaInstances = null,
            Action<string>? reloadOpenAreaInstances = null,
            Func<string, IReadOnlyList<string>>? findUnsavedReferences = null)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _categories = categories;
            _hasUnsavedAreaInstances = hasUnsavedAreaInstances;
            _reloadOpenAreaInstances = reloadOpenAreaInstances;
            _findUnsavedReferences = findUnsavedReferences;
        }

        public BlueprintSaveOutcome Save(
            DocumentSession session,
            ResourceType type,
            string currentResRef,
            string targetResRef)
        {
            ArgumentNullException.ThrowIfNull(session);
            var oldPath = session.FilePath;
            var failed = BlueprintSaveOutcome.Failed(currentResRef, oldPath);
            var renaming = !string.Equals(
                currentResRef,
                targetResRef,
                StringComparison.Ordinal);

            if (!renaming)
            {
                var bytes = session.ToBytes();
                if (!SaveService.TryWriteAtomicIfUnchanged(session, bytes))
                {
                    _log.AppendLine(
                        $"Cannot save {oldPath}: the file changed while the save was being prepared.");
                    return failed;
                }

                return new BlueprintSaveOutcome(
                    true, false, currentResRef, targetResRef, oldPath, oldPath,
                    0, Array.Empty<string>());
            }

            if (!NwnResRef.IsCanonical(targetResRef))
            {
                _log.AppendLine(
                    $"Cannot rename {currentResRef}: ResRef '{targetResRef}' must be " +
                    $"1-{NwnResRef.MaxLength} " +
                    "characters of a-z, 0-9, or underscore.");
                return failed;
            }

            var moduleRoot = ModuleRootFor(oldPath);
            var workspace = new ModuleWorkspace(moduleRoot);
            var newPath = workspace.GetResourcePath(type, targetResRef);
            var caseOnlyRename = string.Equals(
                currentResRef,
                targetResRef,
                StringComparison.OrdinalIgnoreCase);
            if (File.Exists(newPath) &&
                (!caseOnlyRename || HasMultipleCaseVariants(newPath)))
            {
                _log.AppendLine(
                    $"Cannot rename {currentResRef} to {targetResRef}: another blueprint already uses that ResRef.");
                return failed;
            }

            var references = caseOnlyRename
                ? new List<string>()
                : FindBlockingReferences(
                        moduleRoot,
                        type,
                        currentResRef,
                        oldPath)
                    .Concat(_findUnsavedReferences?.Invoke(currentResRef) ?? Array.Empty<string>())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            if (references.Count > 0)
            {
                var shown = string.Join(", ", references.Take(5));
                var more = references.Count > 5 ? $" (+{references.Count - 5} more)" : string.Empty;
                _log.AppendLine(
                    $"Cannot rename {currentResRef} to {targetResRef}: {references.Count} non-instance " +
                    $"reference(s) still use the old ResRef - {shown}{more}. Update those references first.");
                return failed;
            }

            if (_categories != null &&
                !_categories.CanRefileMember(type, currentResRef))
            {
                _log.AppendLine(
                    $"Cannot rename {currentResRef} to {targetResRef}: its category could not be updated.");
                return failed;
            }

            byte[]? originalContentHash;
            if (!session.TryCaptureUnchangedFileContentHash(out originalContentHash))
            {
                _log.AppendLine(
                    $"Cannot rename {currentResRef}: the blueprint changed on disk during preflight.");
                return failed;
            }

            var blueprintBytes = session.ToBytes();
            List<CompanionWrite> companionWrites;
            int updatedInstances;
            IReadOnlyList<string> updatedAreas;
            string? openDirtyArea;
            try
            {
                companionWrites = PrepareInstanceWrites(
                    workspace,
                    type,
                    currentResRef,
                    targetResRef,
                    out updatedInstances,
                    out updatedAreas,
                    out openDirtyArea);
            }
            catch (Exception ex)
            {
                _log.AppendLine(
                    $"Cannot rename {currentResRef}: not every area instance file could be " +
                    $"inspected ({ex.Message}).");
                return failed;
            }
            if (openDirtyArea != null)
            {
                _log.AppendLine(
                    $"Cannot rename {currentResRef}: area '{openDirtyArea}' has unsaved instance edits. " +
                    "Save or revert that area's instance changes, then try again.");
                return failed;
            }

            var companions = companionWrites
                .Select(write => new ItemRenameRecovery.RenameCompanion(
                    write.Path,
                    write.Content,
                    write.OriginalContentHash))
                .ToList();
            var categoryMoved = false;
            ItemRenameRecovery.Transaction? recovery = null;
            try
            {
                recovery = ItemRenameRecovery.Begin(
                    moduleRoot,
                    oldPath,
                    newPath,
                    blueprintBytes,
                    originalContentHash!,
                    companions);

                var staged = new List<SaveService.StagedWrite>();
                try
                {
                    staged.Add(SaveService.StageNew(newPath, blueprintBytes));
                    staged.AddRange(companionWrites.Select(write =>
                        SaveService.Stage(write.Path, write.Content)));

                    // A case-insensitive filesystem exposes the old and new spellings as the same
                    // path, so the destination cannot become "new" until the old directory entry is
                    // removed. The recovery marker and every staged replacement already exist here.
                    if (caseOnlyRename)
                    {
                        if (!recovery.OriginalStillMatches())
                        {
                            throw new IOException(
                                $"The original blueprint '{oldPath}' changed before it could be renamed.");
                        }

                        File.Delete(oldPath);
                    }

                    SaveService.CommitAll(staged);
                }
                catch
                {
                    foreach (var write in staged)
                        SaveService.Discard(write);
                    throw;
                }

                var categoryResult = _categories?.RefileMember(type, currentResRef, targetResRef)
                                     ?? CategorySaveResult.Ok();
                if (!categoryResult.Saved)
                {
                    throw new IOException(
                        categoryResult.Problem ?? "The blueprint category could not be updated.");
                }

                categoryMoved = _categories != null;
                recovery.RecordCategoryGeneration(categoryResult.ContentSha256);
                if (!caseOnlyRename && !recovery.OriginalStillMatches())
                {
                    throw new IOException(
                        $"The original blueprint '{oldPath}' changed before it could be removed.");
                }

                if (!caseOnlyRename)
                    File.Delete(oldPath);
                recovery.Complete();
                session.MoveTo(newPath);

                foreach (var areaResRef in updatedAreas)
                    _reloadOpenAreaInstances?.Invoke(areaResRef);

                return new BlueprintSaveOutcome(
                    true,
                    true,
                    currentResRef,
                    targetResRef,
                    oldPath,
                    newPath,
                    updatedInstances,
                    updatedAreas);
            }
            catch (Exception ex)
            {
                if (categoryMoved)
                    _categories?.RefileMember(type, targetResRef, currentResRef);
                try
                {
                    recovery?.Dispose();
                    recovery = null;
                }
                catch (Exception recoveryFailure)
                {
                    _log.AppendLine(
                        $"Rename failed for {currentResRef}, and automatic recovery also failed: " +
                        recoveryFailure.Message);
                    throw;
                }
                _log.AppendLine(
                    $"Rename failed for {currentResRef}: {ex.Message} Nothing was left partially renamed.");
                return failed;
            }
            finally
            {
                recovery?.Dispose();
            }
        }

        private List<CompanionWrite> PrepareInstanceWrites(
            ModuleWorkspace workspace,
            ResourceType type,
            string currentResRef,
            string targetResRef,
            out int updatedInstances,
            out IReadOnlyList<string> updatedAreas,
            out string? openDirtyArea)
        {
            var writes = new List<CompanionWrite>();
            var areas = new List<string>();
            updatedInstances = 0;
            openDirtyArea = null;
            foreach (var areaResRef in workspace.EnumerateAreaResRefs())
            {
                var path = Path.Combine(workspace.ModuleRoot, "git", areaResRef + ".git.json");
                var originalContent = File.ReadAllBytes(path);
                var git = JsonGffDocument.Parse(originalContent);
                var originalContentHash =
                    System.Security.Cryptography.SHA256.HashData(originalContent);

                int count;
                using (EditScope.EnterConstruction())
                {
                    count = BlueprintInstanceSynchronizer.RenameReferences(
                        type,
                        git,
                        currentResRef,
                        targetResRef);
                }

                if (count == 0)
                    continue;

                if (_hasUnsavedAreaInstances?.Invoke(areaResRef) == true)
                {
                    openDirtyArea = areaResRef;
                    updatedAreas = Array.Empty<string>();
                    return new List<CompanionWrite>();
                }

                writes.Add(new CompanionWrite(path, git.ToBytes(), originalContentHash));
                areas.Add(areaResRef);
                updatedInstances += count;
            }

            updatedAreas = areas;
            return writes;
        }

        private static IReadOnlyList<string> FindBlockingReferences(
            string moduleRoot,
            ResourceType type,
            string resRef,
            string selfFilePath)
        {
            var hits = new List<string>();
            var quoted = $"\"{resRef}\"";
            foreach (var folder in ReferenceFolders)
            {
                var directory = Path.Combine(moduleRoot, folder);
                if (!Directory.Exists(directory))
                    continue;

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);
                }
                catch
                {
                    hits.Add($"Module/{folder} (unscannable - treated as a reference)");
                    continue;
                }

                foreach (var file in files)
                {
                    if (string.Equals(
                            Path.GetFullPath(file),
                            Path.GetFullPath(selfFilePath),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        var text = File.ReadAllText(file);
                        if (ContainsBlockingReference(file, text, quoted, type, resRef))
                        {
                            hits.Add(
                                "Module/" +
                                Path.GetRelativePath(moduleRoot, file).Replace('\\', '/'));
                        }
                    }
                    catch
                    {
                        hits.Add(
                            "Module/" +
                            Path.GetRelativePath(moduleRoot, file).Replace('\\', '/') +
                            " (unreadable - treated as a reference)");
                    }
                }
            }

            var repoRoot = Directory.GetParent(moduleRoot)?.FullName;
            var gameSource = repoRoot == null ? null : Path.Combine(repoRoot, "SWLOR.Game.Server");
            if (gameSource != null && Directory.Exists(gameSource))
                ScanSourceTree(gameSource, "*.cs", quoted, "SWLOR.Game.Server", hits);

            if (type == ResourceType.Uti && repoRoot != null)
            {
                var generatorInputs = Path.Combine(repoRoot, "SWLOR.CLI", "InputFiles");
                if (Directory.Exists(generatorInputs))
                    ScanTokenTree(generatorInputs, resRef, "SWLOR.CLI/InputFiles", hits);
            }

            return hits;
        }

        private static bool ContainsBlockingReference(
            string file,
            string text,
            string quotedResRef,
            ResourceType targetType,
            string resRef)
        {
            var firstOccurrence = text.IndexOf(
                quotedResRef,
                StringComparison.OrdinalIgnoreCase);
            if (firstOccurrence < 0)
                return false;

            var resourceFileName = Path.GetFileNameWithoutExtension(file);
            var resourceExtension = Path.GetExtension(resourceFileName).TrimStart('.');
            if (!ResourceTypeExtensions.TryFromExtension(resourceExtension, out var fileType) ||
                !ModuleWorkspace.BlueprintTypes.Contains(fileType) ||
                fileType == targetType)
            {
                return true;
            }

            var document = JsonGffDocument.Load(file);
            var identityField = InstanceFieldMap.GetInstanceTemplateField(fileType);
            if (!string.Equals(
                    document.Root.GetStringOrNull(identityField),
                    resRef,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Another blueprint type may legitimately own the same ResRef. Ignore that one root
            // identity occurrence, but remain conservative if the value appears anywhere else in
            // the document because that second occurrence can still be a real reference.
            return text.IndexOf(
                quotedResRef,
                firstOccurrence + quotedResRef.Length,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ScanTokenTree(
            string root,
            string resRef,
            string displayRoot,
            List<string> hits)
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories);
            }
            catch
            {
                hits.Add(displayRoot + " (unscannable - treated as a reference)");
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    if (ContainsResRefToken(File.ReadAllText(file), resRef))
                    {
                        hits.Add(
                            displayRoot + "/" +
                            Path.GetRelativePath(root, file).Replace('\\', '/'));
                    }
                }
                catch
                {
                    hits.Add(
                        displayRoot + "/" +
                        Path.GetRelativePath(root, file).Replace('\\', '/') +
                        " (unreadable - treated as a reference)");
                }
            }
        }

        private static bool ContainsResRefToken(string text, string resRef)
        {
            var searchFrom = 0;
            while (searchFrom < text.Length)
            {
                var index = text.IndexOf(resRef, searchFrom, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    return false;

                var beforeIsToken = index > 0 && IsResRefCharacter(text[index - 1]);
                var after = index + resRef.Length;
                var afterIsToken = after < text.Length && IsResRefCharacter(text[after]);
                if (!beforeIsToken && !afterIsToken)
                    return true;
                searchFrom = index + resRef.Length;
            }

            return false;
        }

        private static bool IsResRefCharacter(char value) =>
            char.IsAsciiLetterOrDigit(value) || value == '_';

        private static void ScanSourceTree(
            string root,
            string pattern,
            string quoted,
            string displayRoot,
            List<string> hits)
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories);
            }
            catch
            {
                hits.Add(displayRoot + " (unscannable - treated as a reference)");
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    if (File.ReadAllText(file).Contains(quoted, StringComparison.OrdinalIgnoreCase))
                    {
                        hits.Add(
                            displayRoot + "/" +
                            Path.GetRelativePath(root, file).Replace('\\', '/'));
                    }
                }
                catch
                {
                    hits.Add(
                        displayRoot + "/" +
                        Path.GetRelativePath(root, file).Replace('\\', '/') +
                        " (unreadable - treated as a reference)");
                }
            }
        }

        private static string ModuleRootFor(string resourcePath)
        {
            var resourceDirectory = Path.GetDirectoryName(Path.GetFullPath(resourcePath))
                                    ?? throw new InvalidOperationException(
                                        $"Could not determine the resource folder for '{resourcePath}'.");
            return Directory.GetParent(resourceDirectory)?.FullName
                   ?? throw new InvalidOperationException(
                       $"Could not determine the module root for '{resourcePath}'.");
        }

        private static bool HasMultipleCaseVariants(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (directory == null || !Directory.Exists(directory))
                return false;

            var fileName = Path.GetFileName(path);
            return Directory.EnumerateFiles(directory)
                .Count(candidate => string.Equals(
                    Path.GetFileName(candidate),
                    fileName,
                    StringComparison.OrdinalIgnoreCase)) > 1;
        }

        private sealed record CompanionWrite(
            string Path,
            byte[] Content,
            byte[] OriginalContentHash);
    }
}
