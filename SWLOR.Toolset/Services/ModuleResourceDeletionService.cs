using System.Security.Cryptography;
using System.Text.Json;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// The immutable generation of one Module Contents resource that the builder agreed to delete.
    /// Preparing before the confirmation and verifying again under the module lease prevents the
    /// confirmation from deleting a newer file that appeared while the dialog was open.
    /// </summary>
    public sealed class ModuleResourceDeletionPlan
    {
        internal ModuleResourceDeletionPlan(
            string moduleRoot,
            ResourceType type,
            string resRef,
            IReadOnlyList<DeletionPathState> paths,
            IReadOnlyList<string> lockRoots,
            string? ifoPath,
            byte[]? expectedIfo,
            byte[]? updatedIfo,
            bool removesAreaRegistration)
        {
            ModuleRoot = moduleRoot;
            Type = type;
            ResRef = resRef;
            Paths = paths;
            LockRoots = lockRoots;
            IfoPath = ifoPath;
            ExpectedIfo = expectedIfo;
            UpdatedIfo = updatedIfo;
            RemovesAreaRegistration = removesAreaRegistration;
        }

        internal string ModuleRoot { get; }

        public ResourceType Type { get; }

        public string ResRef { get; }

        /// <summary>The resource files that existed when the confirmation was opened.</summary>
        public IReadOnlyList<string> ExistingFileNames => Paths
            .Where(path => path.Existed)
            .Select(path => Path.GetFileName(path.Path))
            .ToList();

        /// <summary>Whether committing this plan also removes one or more module.ifo area entries.</summary>
        public bool RemovesAreaRegistration { get; }

        internal IReadOnlyList<DeletionPathState> Paths { get; }
        internal IReadOnlyList<string> LockRoots { get; }
        internal string? IfoPath { get; }
        internal byte[]? ExpectedIfo { get; }
        internal byte[]? UpdatedIfo { get; }
    }

    /// <summary>The completed delete and any transaction backups that could not be tidied.</summary>
    public readonly record struct ModuleResourceDeletionResult(
        IReadOnlyList<string> DeletedPaths,
        IReadOnlyList<string> CleanupWarnings);

    /// <summary>
    /// Raised when an interrupted resource delete cannot be rolled back without risking a newer
    /// filesystem generation. Opening and packing must stop until the named transaction is repaired.
    /// </summary>
    public sealed class ModuleResourceDeleteRecoveryException(string manifestPath, Exception innerException)
        : IOException(
            $"Could not recover interrupted resource delete '{manifestPath}': {innerException.Message}",
            innerException);

    /// <summary>
    /// Deletes the logical resources shown by Module Contents: an ARE/GIT/GIC area plus its IFO
    /// registration, either form of a conversation, or NSS source plus its compiled NCS artifact.
    /// </summary>
    public static class ModuleResourceDeletionService
    {
        internal const string DeleteBackupSuffix = ".delete-backup";
        internal const string DeleteTransactionSuffix = ".resource-delete-transaction.json";
        private const int DeleteTransactionVersion = 1;

        /// <summary>
        /// Captures every file generation affected by a delete. The returned plan must be committed
        /// only after the builder confirms the destructive action.
        /// </summary>
        public static ModuleResourceDeletionPlan Prepare(
            ModuleWorkspace workspace,
            ResourceType type,
            string resRef)
        {
            ArgumentNullException.ThrowIfNull(workspace);
            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must be provided.", nameof(resRef));
            if (type is not (ResourceType.Area or ResourceType.Dlg or ResourceType.Nss))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Not a Module Contents resource type.");

            if (type == ResourceType.Area &&
                resRef.Equals(NewAreaWriter.TemplateResRef, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"'{resRef}' is the template used to create new areas and cannot be deleted.");
            }

            var paths = PathsFor(workspace, type, resRef)
                .Select(DeletionPathState.Capture)
                .ToList();
            if (!PrimaryExists(type, paths))
            {
                throw new FileNotFoundException(
                    $"The {type.SingularDisplayName().ToLowerInvariant()} '{resRef}' no longer exists.");
            }

            string? ifoPath = null;
            byte[]? expectedIfo = null;
            byte[]? updatedIfo = null;
            var removesAreaRegistration = false;
            if (type == ResourceType.Area)
            {
                ifoPath = Path.Combine(workspace.ModuleRoot, "ifo", "module.ifo.json");
                expectedIfo = File.ReadAllBytes(ifoPath);
                var ifo = IfoDocument.Parse(expectedIfo);
                if (string.Equals(ifo.EntryArea, resRef, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"'{resRef}' is the module entry area. Choose another entry area before deleting it.");
                }

                removesAreaRegistration = AreaTemplateFactory.RemoveAreaFromModule(ifo, resRef) > 0;
                if (removesAreaRegistration)
                    updatedIfo = ifo.ToBytes();
            }

            // PackService takes the conversation source lease before the module lease. Use the same
            // order here when a logical dialog can span both roots, or the two processes can deadlock.
            var lockRoots = type == ResourceType.Dlg
                ? new[] { workspace.ConversationDataRoot, workspace.ModuleRoot }
                : new[] { workspace.ModuleRoot };

            return new ModuleResourceDeletionPlan(
                workspace.ModuleRoot,
                type,
                resRef,
                paths,
                lockRoots.Distinct(PathComparer).ToList(),
                ifoPath,
                expectedIfo,
                updatedIfo,
                removesAreaRegistration);
        }

        /// <summary>
        /// Revalidates and commits a prepared delete under the same cross-process leases used by
        /// module saves and packing. Multi-file resources are first moved to transaction backups;
        /// any failure restores every moved file and the original IFO generation.
        /// </summary>
        public static ModuleResourceDeletionResult Commit(ModuleResourceDeletionPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);
            ModuleMutationLock.ThrowIfModuleLocked();

            using var leases = ModuleLeaseSet.Acquire(plan.LockRoots);
            ModuleMutationLock.ThrowIfModuleLocked();
            using var ifoLease = plan.IfoPath == null
                ? null
                : ModuleIfoUpdateLock.Acquire(Path.GetDirectoryName(Path.GetDirectoryName(plan.IfoPath))!);

            foreach (var path in plan.Paths)
                path.VerifyUnchanged();
            if (plan.IfoPath != null && plan.ExpectedIfo != null)
                VerifyBytes(plan.IfoPath, plan.ExpectedIfo);

            var transactionId = Guid.NewGuid().ToString("N");
            var manifest = BuildManifest(plan, transactionId);
            var manifestPath = TransactionManifestPath(plan.ModuleRoot, transactionId);
            var moved = new List<(string Source, string Backup)>();
            var ifoUpdated = false;
            try
            {
                // The manifest is durable before the first destructive move. If the process exits
                // anywhere below, startup and packing roll the whole logical resource back instead
                // of exposing only the companions that had not moved yet.
                WriteManifest(manifestPath, manifest);

                // Removing the registration first leaves an interrupted area delete with harmless
                // orphan files, never a registered area whose required triplet is missing.
                if (plan.IfoPath != null && plan.UpdatedIfo != null)
                {
                    ModuleMutationLock.ThrowIfModuleLocked();
                    WriteAtomicUnderLease(plan.IfoPath, plan.UpdatedIfo);
                    ifoUpdated = true;
                }

                foreach (var path in plan.Paths.Where(path => path.Existed))
                {
                    var backup = path.Path + "." + transactionId + DeleteBackupSuffix;
                    File.Move(path.Path, backup, overwrite: false);
                    moved.Add((path.Path, backup));
                }

                // This is the commit point. While the manifest exists recovery restores the old
                // generation. Once every companion has moved, deleting it declares the resource
                // gone; leftover backups are then only cleanup debris.
                File.Delete(manifestPath);
            }
            catch (Exception failure)
            {
                var rollbackFailures = RollBack(plan, moved, ifoUpdated);
                if (rollbackFailures.Count > 0)
                {
                    throw new IOException(
                        $"Deleting '{plan.ResRef}' failed ({failure.Message}), and rollback also failed: " +
                        string.Join("; ", rollbackFailures),
                        failure);
                }

                TryDeleteManifest(manifestPath);
                throw;
            }

            var cleanupWarnings = new List<string>();
            foreach (var (_, backup) in moved)
            {
                try
                {
                    File.Delete(backup);
                }
                catch (Exception ex)
                {
                    cleanupWarnings.Add($"{backup}: {ex.Message}");
                }
            }

            return new ModuleResourceDeletionResult(
                moved.Select(item => item.Source).ToList(),
                cleanupWarnings);
        }

        /// <summary>
        /// Rolls back resource deletes whose durable manifest survived a process exit. Recovery is
        /// run before a module is opened or packed, under the same conversation/module lease order
        /// as normal deletes, so neither consumer can observe a partial companion set.
        /// </summary>
        /// <returns>The logical resources restored from interrupted transactions.</returns>
        public static IReadOnlyList<string> RecoverInterruptedDeletes(string moduleRoot)
        {
            if (string.IsNullOrWhiteSpace(moduleRoot) || !Directory.Exists(moduleRoot))
                return Array.Empty<string>();

            moduleRoot = Path.GetFullPath(moduleRoot);
            var conversationRoot = ModuleWorkspace.ResolveConversationDataRoot(moduleRoot);
            using var leases = ModuleLeaseSet.Acquire(new[] { conversationRoot, moduleRoot }
                .Distinct(PathComparer));
            using var ifoLease = ModuleIfoUpdateLock.Acquire(moduleRoot);

            var recovered = new List<string>();
            foreach (var manifestPath in Directory.EnumerateFiles(
                         moduleRoot,
                         ".*" + DeleteTransactionSuffix,
                         SearchOption.TopDirectoryOnly))
            {
                DeleteTransactionManifest manifest;
                try
                {
                    manifest = JsonSerializer.Deserialize<DeleteTransactionManifest>(
                                   File.ReadAllText(manifestPath))
                               ?? throw new InvalidDataException("manifest is empty");
                    ValidateManifest(moduleRoot, conversationRoot, manifestPath, manifest);
                    RecoverManifest(manifestPath, manifest);
                    recovered.Add($"{manifest.Type.ToLowerInvariant()} '{manifest.ResRef}'");
                }
                catch (Exception ex) when (ex is not ModuleResourceDeleteRecoveryException)
                {
                    throw new ModuleResourceDeleteRecoveryException(manifestPath, ex);
                }
            }

            return recovered;
        }

        private static DeleteTransactionManifest BuildManifest(
            ModuleResourceDeletionPlan plan,
            string transactionId) => new()
        {
            Version = DeleteTransactionVersion,
            TransactionId = transactionId,
            ModuleRoot = Path.GetFullPath(plan.ModuleRoot),
            Type = plan.Type.ToString(),
            ResRef = plan.ResRef,
            Entries = plan.Paths
                .Where(path => path.Existed)
                .Select(path => new DeleteTransactionEntry
                {
                    SourcePath = Path.GetFullPath(path.Path),
                    BackupPath = Path.GetFullPath(
                        path.Path + "." + transactionId + DeleteBackupSuffix),
                    SourceSha256 = Convert.ToHexString(path.Sha256!)
                })
                .ToList(),
            IfoPath = plan.IfoPath == null ? null : Path.GetFullPath(plan.IfoPath),
            ExpectedIfoBase64 = plan.ExpectedIfo == null
                ? null
                : Convert.ToBase64String(plan.ExpectedIfo),
            UpdatedIfoSha256 = plan.UpdatedIfo == null
                ? null
                : Convert.ToHexString(SHA256.HashData(plan.UpdatedIfo))
        };

        private static string TransactionManifestPath(string moduleRoot, string transactionId) =>
            Path.Combine(moduleRoot, "." + transactionId + DeleteTransactionSuffix);

        private static void WriteManifest(string manifestPath, DeleteTransactionManifest manifest)
        {
            var temporaryPath = manifestPath + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(manifest));
                File.Move(temporaryPath, manifestPath, overwrite: false);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static void TryDeleteManifest(string manifestPath)
        {
            try
            {
                if (File.Exists(manifestPath))
                    File.Delete(manifestPath);
            }
            catch (IOException)
            {
                // A fully rolled-back transaction is safe to recover again at the next open/pack.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void ValidateManifest(
            string moduleRoot,
            string conversationRoot,
            string manifestPath,
            DeleteTransactionManifest manifest)
        {
            if (manifest.Version != DeleteTransactionVersion)
                throw new InvalidDataException($"unsupported manifest version {manifest.Version}");
            if (!Guid.TryParseExact(manifest.TransactionId, "N", out _))
                throw new InvalidDataException("transaction id is invalid");

            var expectedManifestPath = TransactionManifestPath(moduleRoot, manifest.TransactionId);
            if (!PathsEqual(expectedManifestPath, manifestPath))
                throw new InvalidDataException("transaction id does not match the manifest filename");
            if (!PathsEqual(moduleRoot, manifest.ModuleRoot))
                throw new InvalidDataException("manifest belongs to a different module root");
            if (!Enum.TryParse<ResourceType>(manifest.Type, ignoreCase: false, out var type) ||
                type is not (ResourceType.Area or ResourceType.Dlg or ResourceType.Nss))
            {
                throw new InvalidDataException("resource type is invalid");
            }
            if (string.IsNullOrWhiteSpace(manifest.ResRef))
                throw new InvalidDataException("resource ResRef is missing");
            if (manifest.Entries.Count == 0)
                throw new InvalidDataException("manifest contains no resource files");

            var seenSources = new HashSet<string>(PathComparer);
            foreach (var entry in manifest.Entries)
            {
                var sourcePath = CanonicalManifestPath(entry.SourcePath, "source");
                var backupPath = CanonicalManifestPath(entry.BackupPath, "backup");
                if (!IsPathUnderRoot(moduleRoot, sourcePath) &&
                    !IsPathUnderRoot(conversationRoot, sourcePath))
                {
                    throw new InvalidDataException($"source path escapes the resource roots: {sourcePath}");
                }

                var expectedBackup = sourcePath + "." + manifest.TransactionId + DeleteBackupSuffix;
                if (!PathsEqual(expectedBackup, backupPath))
                    throw new InvalidDataException($"backup path does not match its source: {backupPath}");
                if (!seenSources.Add(sourcePath))
                    throw new InvalidDataException($"source path is duplicated: {sourcePath}");
                ValidateSha256(entry.SourceSha256, "resource");
            }

            if (manifest.IfoPath == null)
            {
                if (manifest.ExpectedIfoBase64 != null || manifest.UpdatedIfoSha256 != null)
                    throw new InvalidDataException("IFO recovery data has no IFO path");
                return;
            }

            var expectedIfoPath = Path.Combine(moduleRoot, "ifo", "module.ifo.json");
            if (!PathsEqual(expectedIfoPath, manifest.IfoPath))
                throw new InvalidDataException("IFO path is not this module's module.ifo.json");
            if (manifest.ExpectedIfoBase64 == null)
                throw new InvalidDataException("original IFO generation is missing");
            try
            {
                _ = Convert.FromBase64String(manifest.ExpectedIfoBase64);
            }
            catch (FormatException ex)
            {
                throw new InvalidDataException("original IFO generation is invalid", ex);
            }

            if (manifest.UpdatedIfoSha256 != null)
                ValidateSha256(manifest.UpdatedIfoSha256, "updated IFO");
        }

        private static void RecoverManifest(
            string manifestPath,
            DeleteTransactionManifest manifest)
        {
            foreach (var entry in manifest.Entries)
            {
                var sourceExists = File.Exists(entry.SourcePath);
                var backupExists = File.Exists(entry.BackupPath);
                if (sourceExists == backupExists)
                {
                    var state = sourceExists
                        ? "both the source and backup exist"
                        : "both the source and backup are missing";
                    throw new IOException($"cannot restore '{entry.SourcePath}': {state}");
                }

                var survivingPath = backupExists ? entry.BackupPath : entry.SourcePath;
                VerifySha256(survivingPath, entry.SourceSha256);
            }

            byte[]? expectedIfo = null;
            var restoreIfo = false;
            if (manifest.IfoPath != null && manifest.ExpectedIfoBase64 != null)
            {
                if (!File.Exists(manifest.IfoPath))
                    throw new FileNotFoundException("module.ifo.json is missing", manifest.IfoPath);

                expectedIfo = Convert.FromBase64String(manifest.ExpectedIfoBase64);
                var currentIfo = File.ReadAllBytes(manifest.IfoPath);
                if (currentIfo.AsSpan().SequenceEqual(expectedIfo))
                {
                    restoreIfo = false;
                }
                else if (manifest.UpdatedIfoSha256 != null &&
                         Convert.ToHexString(SHA256.HashData(currentIfo))
                             .Equals(manifest.UpdatedIfoSha256, StringComparison.OrdinalIgnoreCase))
                {
                    restoreIfo = true;
                }
                else
                {
                    throw new IOException(
                        "module.ifo.json changed after the interrupted delete; automatic recovery was refused");
                }
            }

            for (var index = manifest.Entries.Count - 1; index >= 0; index--)
            {
                var entry = manifest.Entries[index];
                if (File.Exists(entry.BackupPath))
                    File.Move(entry.BackupPath, entry.SourcePath, overwrite: false);
            }

            if (restoreIfo)
                WriteAtomicUnderLease(manifest.IfoPath!, expectedIfo!);

            File.Delete(manifestPath);
        }

        private static string CanonicalManifestPath(string path, string description)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidDataException($"{description} path is missing");
            var canonical = Path.GetFullPath(path);
            if (!PathsEqual(canonical, path))
                throw new InvalidDataException($"{description} path is not canonical: {path}");
            return canonical;
        }

        private static bool IsPathUnderRoot(string root, string candidate)
        {
            var normalizedRoot = Path.GetFullPath(root).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var normalizedCandidate = Path.GetFullPath(candidate);
            return normalizedCandidate.StartsWith(
                normalizedRoot,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal);
        }

        private static bool PathsEqual(string left, string right) =>
            string.Equals(Path.GetFullPath(left), Path.GetFullPath(right),
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal);

        private static void ValidateSha256(string value, string description)
        {
            if (value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
                throw new InvalidDataException($"{description} SHA-256 is invalid");
        }

        private static void VerifySha256(string path, string expected)
        {
            var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                throw new IOException($"'{path}' changed after the interrupted delete");
        }

        private static IReadOnlyList<string> PathsFor(
            ModuleWorkspace workspace,
            ResourceType type,
            string resRef) => type switch
        {
            ResourceType.Area => new[]
            {
                workspace.GetResourcePath(ResourceType.Area, resRef),
                Path.Combine(workspace.ModuleRoot, "git", resRef + ".git.json"),
                Path.Combine(workspace.ModuleRoot, "gic", resRef + ".gic.json")
            },
            ResourceType.Dlg => new[]
            {
                workspace.GetConversationGraphPath(resRef),
                workspace.GetResourcePath(ResourceType.Dlg, resRef)
            },
            ResourceType.Nss => new[]
            {
                workspace.GetResourcePath(ResourceType.Nss, resRef),
                Path.Combine(workspace.ModuleRoot, "ncs", resRef + ".ncs")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        private static bool PrimaryExists(
            ResourceType type,
            IReadOnlyList<DeletionPathState> paths) => type switch
        {
            ResourceType.Dlg => paths.Any(path => path.Existed),
            _ => paths[0].Existed
        };

        private static List<string> RollBack(
            ModuleResourceDeletionPlan plan,
            IReadOnlyList<(string Source, string Backup)> moved,
            bool ifoUpdated)
        {
            var failures = new List<string>();
            for (var index = moved.Count - 1; index >= 0; index--)
            {
                var (source, backup) = moved[index];
                try
                {
                    if (File.Exists(source))
                        throw new IOException($"a new file appeared at '{source}'");
                    File.Move(backup, source, overwrite: false);
                }
                catch (Exception ex)
                {
                    failures.Add($"could not restore {Path.GetFileName(source)} ({ex.Message})");
                }
            }

            if (ifoUpdated && plan.IfoPath != null && plan.ExpectedIfo != null)
            {
                try
                {
                    WriteAtomicUnderLease(plan.IfoPath, plan.ExpectedIfo);
                }
                catch (Exception ex)
                {
                    failures.Add($"could not restore module.ifo.json ({ex.Message})");
                }
            }

            return failures;
        }

        private static void VerifyBytes(string path, byte[] expected)
        {
            if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(expected))
            {
                throw new IOException(
                    $"{Path.GetFileName(path)} changed while the delete confirmation was open. " +
                    "Refresh Module Contents and try again.");
            }
        }

        private static void WriteAtomicUnderLease(string path, byte[] bytes)
        {
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporary, bytes);
                File.Move(temporary, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        private static StringComparer PathComparer =>
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        private sealed class DeleteTransactionManifest
        {
            public int Version { get; set; }
            public string TransactionId { get; set; } = string.Empty;
            public string ModuleRoot { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string ResRef { get; set; } = string.Empty;
            public List<DeleteTransactionEntry> Entries { get; set; } = new();
            public string? IfoPath { get; set; }
            public string? ExpectedIfoBase64 { get; set; }
            public string? UpdatedIfoSha256 { get; set; }
        }

        private sealed class DeleteTransactionEntry
        {
            public string SourcePath { get; set; } = string.Empty;
            public string BackupPath { get; set; } = string.Empty;
            public string SourceSha256 { get; set; } = string.Empty;
        }

        private sealed class ModuleLeaseSet : IDisposable
        {
            private readonly List<ModuleWriteLock> _leases;

            private ModuleLeaseSet(List<ModuleWriteLock> leases)
            {
                _leases = leases;
            }

            public static ModuleLeaseSet Acquire(IEnumerable<string> roots)
            {
                var leases = new List<ModuleWriteLock>();
                try
                {
                    foreach (var root in roots)
                        leases.Add(ModuleWriteLock.Acquire(root));
                    return new ModuleLeaseSet(leases);
                }
                catch
                {
                    for (var index = leases.Count - 1; index >= 0; index--)
                        leases[index].Dispose();
                    throw;
                }
            }

            public void Dispose()
            {
                for (var index = _leases.Count - 1; index >= 0; index--)
                    _leases[index].Dispose();
            }
        }
    }

    internal sealed record DeletionPathState(string Path, bool Existed, byte[]? Sha256)
    {
        public static DeletionPathState Capture(string path)
        {
            if (!File.Exists(path))
                return new DeletionPathState(path, false, null);

            return new DeletionPathState(path, true, SHA256.HashData(File.ReadAllBytes(path)));
        }

        public void VerifyUnchanged()
        {
            var existsNow = File.Exists(Path);
            var unchanged = existsNow == Existed &&
                            (!Existed || SHA256.HashData(File.ReadAllBytes(Path))
                                .AsSpan()
                                .SequenceEqual(Sha256));
            if (!unchanged)
            {
                throw new IOException(
                    $"{System.IO.Path.GetFileName(Path)} changed while the delete confirmation was open. " +
                    "Refresh Module Contents and try again.");
            }
        }
    }
}
