using System.Security.Cryptography;
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
            ResourceType type,
            string resRef,
            IReadOnlyList<DeletionPathState> paths,
            IReadOnlyList<string> lockRoots,
            string? ifoPath,
            byte[]? expectedIfo,
            byte[]? updatedIfo,
            bool removesAreaRegistration)
        {
            Type = type;
            ResRef = resRef;
            Paths = paths;
            LockRoots = lockRoots;
            IfoPath = ifoPath;
            ExpectedIfo = expectedIfo;
            UpdatedIfo = updatedIfo;
            RemovesAreaRegistration = removesAreaRegistration;
        }

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
    /// Deletes the logical resources shown by Module Contents: an ARE/GIT/GIC area plus its IFO
    /// registration, either form of a conversation, or NSS source plus its compiled NCS artifact.
    /// </summary>
    public static class ModuleResourceDeletionService
    {
        private const string DeleteBackupSuffix = ".delete-backup";

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
            var moved = new List<(string Source, string Backup)>();
            var ifoUpdated = false;
            try
            {
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
