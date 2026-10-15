using Nwn.Authoring.Editing;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using Nwn.Authoring.Documents.Native;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// Deletes the logical resources shown by Module Contents: an ARE/GIT/GIC area plus its IFO
    /// registration, either form of a conversation, or NSS source plus its compiled NCS artifact.
    /// </summary>
    public static class ModuleResourceDeletionService
    {
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

            var paths = PathsFor(workspace, type, resRef);
            var primaryExists = type == ResourceType.Dlg
                ? paths.Any(File.Exists)
                : File.Exists(paths[0]);
            if (!primaryExists)
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

            var conversationRoot = workspace.ConversationDataRoot;
            if (type == ResourceType.Dlg)
                Directory.CreateDirectory(conversationRoot);

            // PackService takes the conversation source lease before the module lease. Preserve that
            // order for both committing and recovering a transaction that spans the two roots.
            var lockRoots = type == ResourceType.Dlg
                ? new[] { conversationRoot, workspace.ModuleRoot }
                : new[] { workspace.ModuleRoot };
            var allowedRoots = type == ResourceType.Dlg
                ? new[] { conversationRoot, workspace.ModuleRoot }
                : new[] { workspace.ModuleRoot };
            var transactionRoot = type == ResourceType.Dlg ? conversationRoot : workspace.ModuleRoot;
            var replacements = ifoPath != null && updatedIfo != null
                ? new[] { new FileTransactionReplacement(ifoPath, expectedIfo!, updatedIfo) }
                : Array.Empty<FileTransactionReplacement>();
            var transactionPlan = FileTransactionPlan.Capture(
                transactionRoot,
                allowedRoots,
                paths,
                replacements);

            return new ModuleResourceDeletionPlan(
                workspace.ModuleRoot,
                type,
                resRef,
                transactionPlan,
                lockRoots.Distinct(PathComparer).ToArray(),
                ifoPath,
                expectedIfo,
                removesAreaRegistration);
        }

        /// <summary>
        /// Revalidates and commits a prepared delete under the same cross-process leases used by
        /// module saves and packing. The shared transaction restores every original generation if
        /// any companion or guarded IFO replacement fails.
        /// </summary>
        public static ModuleResourceDeletionResult Commit(ModuleResourceDeletionPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);
            ModuleMutationLock.ThrowIfModuleLocked();

            using var leases = ModuleLeaseSet.Acquire(plan.LockRoots);
            ModuleMutationLock.ThrowIfModuleLocked();
            using var ifoLease = plan.IfoPath == null
                ? null
                : ModuleIfoUpdateLock.Acquire(plan.ModuleRoot);

            if (plan.IfoPath != null && plan.ExpectedIfo != null)
                VerifyBytes(plan.IfoPath, plan.ExpectedIfo);

            var committed = FileTransaction.Commit(plan.FileTransactionPlan);
            return new ModuleResourceDeletionResult(committed.DeletedPaths, committed.CleanupWarnings);
        }

        /// <summary>
        /// Recovers shared transactions and legacy deletion journals before module enumeration or
        /// packing. Module-only journals and cross-root conversation journals use distinct roots and
        /// exact allowed-root sets, under the same conversation/module/IFO lock order as deletion.
        /// </summary>
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
            try
            {
                recovered.AddRange(FileTransaction.RecoverInterrupted(moduleRoot, new[] { moduleRoot })
                    .Select(path => $"file '{Path.GetFileName(path)}'"));
                if (Directory.Exists(conversationRoot))
                {
                    recovered.AddRange(FileTransaction.RecoverInterrupted(
                            conversationRoot,
                            new[] { conversationRoot, moduleRoot })
                        .Select(path => $"file '{Path.GetFileName(path)}'"));
                }
            }
            catch (FileTransactionRecoveryException exception)
            {
                throw new ModuleResourceDeleteRecoveryException(exception.ManifestPath, exception);
            }

            recovered.AddRange(ModuleResourceDeletionLegacyRecovery.Recover(moduleRoot, conversationRoot));
            return recovered;
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

        private static void VerifyBytes(string path, byte[] expected)
        {
            if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(expected))
            {
                throw new IOException(
                    $"{Path.GetFileName(path)} changed while the delete confirmation was open. " +
                    "Refresh Module Contents and try again.");
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
}
