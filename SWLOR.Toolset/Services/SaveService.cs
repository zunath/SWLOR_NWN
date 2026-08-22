using System.Security.Cryptography;
using System.Text.Json;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// The one save path for editor sessions: serializes the document (byte-faithful for
    /// untouched content by construction) and writes atomically via a temp file. EOL style and
    /// trailing-newline state are preserved because JsonGffDocument carries them.
    /// </summary>
    public sealed class SaveService
    {
        private readonly OutputLogService _log;

        public SaveService(OutputLogService log)
        {
            _log = log;
        }

        /// <summary>Saves the session if dirty; returns true when the file is clean afterwards.</summary>
        public bool Save(DocumentSession session)
        {
            if (!session.UndoStack.IsDirty)
                return true;

            try
            {
                var saveBytes = session.ToBytes();
                if (!TryWriteAtomicIfUnchanged(session, saveBytes))
                {
                    _log.AppendLine(
                        $"Save refused for {session.FilePath}: the file changed outside the editor.");
                    return false;
                }
                session.UndoStack.MarkSaved();
                session.RecordCurrentFileState(saveBytes);
                _log.AppendLine($"Saved {session.FilePath}.");
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {session.FilePath}: {ex.Message}");
                return false;
            }
        }

        public static void WriteAtomic(string path, byte[] bytes)
        {
            var staged = Stage(path, bytes);
            try
            {
                Commit(staged);
            }
            catch
            {
                // The staged file is transaction debris once the commit has failed - the target is
                // untouched, so nothing needs it. Left behind, a "foo.nss.tmp" sits next to the
                // script until someone notices, and ModulePacker copies whatever it finds in the
                // script folders.
                Discard(staged);
                throw;
            }
        }

        /// <summary>
        /// Replaces one session file only if it still matches the generation the editor accepted.
        /// The final fingerprint check and atomic replacement share one module-wide lease, so a
        /// second toolset or CLI process cannot write between them.
        /// </summary>
        public static bool TryWriteAtomicIfUnchanged(DocumentSession session, byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(bytes);

            ModuleMutationLock.ThrowIfModuleLocked();
            using var moduleWriteLock =
                ModuleWriteLock.AcquireForResourcePath(session.FilePath, TimeSpan.Zero);
            if (session.HasExternalChange())
                return false;

            WriteAtomic(session.FilePath, bytes);
            return true;
        }

        /// <summary>
        /// Replaces a script source only if it still matches the generation the editor accepted.
        /// The final fingerprint check and atomic replacement share one module-wide lease.
        /// </summary>
        public static bool TryWriteAtomicIfUnchanged(ScriptSession session, byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(bytes);

            ModuleMutationLock.ThrowIfModuleLocked();
            using var moduleWriteLock =
                ModuleWriteLock.AcquireForResourcePath(session.FilePath, TimeSpan.Zero);
            if (session.HasExternalChange())
                return false;

            WriteAtomic(session.FilePath, bytes);
            return true;
        }

        /// <summary>
        /// <see cref="WriteAtomic"/> for a path that must NOT exist yet - a rename's destination.
        /// An ordinary save owns its target and may replace it, but a rename's target was only
        /// checked before a potentially long preflight (the reference scan); if another process
        /// created that blueprint in the meantime, overwriting it would silently destroy it. The
        /// no-overwrite move makes the race lose loudly instead: an <see cref="IOException"/>
        /// fails the save and the freshly appeared file survives.
        /// </summary>
        public static void WriteAtomicNew(string path, byte[] bytes)
        {
            var staged = Stage(path, bytes);
            try
            {
                ModuleMutationLock.ThrowIfModuleLocked();
                using var moduleWriteLock = ModuleWriteLock.AcquireForResourcePath(path);
                File.Move(staged.TemporaryPath, staged.TargetPath, overwrite: false);
            }
            catch
            {
                Discard(staged);
                throw;
            }
        }

        /// <summary>The suffix a grouped save gives an original while its replacement is installed.</summary>
        public const string BackupSuffix = ".save-backup";

        /// <summary>The suffix of the recovery manifest that keeps a grouped save indivisible.</summary>
        public const string TransactionSuffix = ".save-transaction.json";

        /// <summary>
        /// Puts back any canonical file a grouped save was interrupted mid-way through replacing.
        /// </summary>
        /// <remarks>
        /// <see cref="CommitAll"/> moves each original aside before installing its replacement, and
        /// rolls the whole group back if a later one fails. What it cannot roll back is the process
        /// being killed between those two moves: the original is then only at its backup path, and
        /// the canonical ARE, GIT, or GIC is simply gone. Nothing looked for these afterwards - the
        /// packer and the file watcher both skip them - so the next open failed on a missing file
        /// while the one surviving copy sat beside it.
        /// </remarks>
        /// <returns>The canonical paths restored.</returns>
        public static IReadOnlyList<string> RecoverInterruptedSaves(string moduleRoot)
        {
            if (string.IsNullOrWhiteSpace(moduleRoot) || !Directory.Exists(moduleRoot))
                return Array.Empty<string>();

            using var moduleWriteLock = ModuleWriteLock.Acquire(moduleRoot);
            var restored = new List<string>();
            var protectedBackups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var protectedTransactionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // A manifest whose group could not be fully restored (a locked backup or target, most
            // often) is collected here rather than silently left "incomplete": every entry describes
            // exactly which files are still at risk, so the exception thrown below can name them.
            var incompleteTransactions = new List<string>();

            foreach (var manifestPath in Directory.EnumerateFiles(
                         moduleRoot, "*" + TransactionSuffix, SearchOption.AllDirectories))
            {
                // A manifest that cannot be trusted - unreadable, or valid JSON that carries no
                // entries (a bare "null" or "{}") - is transaction evidence, not litter. Leave it
                // alone and shield its transaction's backups (identifiable by the id embedded in
                // ".<id>.save-transaction.json" / "<target>.<id>.save-backup") from the orphan
                // sweep below, which would otherwise restore or delete group members piecemeal
                // and leave an ARE/GIT/GIC group at mixed generations.
                void ProtectTransaction()
                {
                    var manifestName = Path.GetFileName(manifestPath);
                    if (manifestName.StartsWith('.') &&
                        manifestName.Length > 1 + TransactionSuffix.Length)
                    {
                        protectedTransactionIds.Add(manifestName[1..^TransactionSuffix.Length]);
                    }
                }

                SaveTransactionManifest? manifest;
                try
                {
                    manifest = JsonSerializer.Deserialize<SaveTransactionManifest>(
                        File.ReadAllText(manifestPath));
                }
                catch (Exception)
                {
                    ProtectTransaction();
                    // Protecting the backups is only half of what an untrustworthy manifest demands.
                    // A moved ARE/GIT/GIC target whose only surviving copy is one of those protected
                    // backups is still missing from its canonical path - without this, recovery
                    // reported success and WorkspaceContext.Open continued past a group that is, in
                    // fact, still incomplete.
                    incompleteTransactions.Add($"{manifestPath} (manifest unreadable)");
                    continue;
                }

                if (manifest?.Entries.Count is not > 0)
                {
                    ProtectTransaction();
                    incompleteTransactions.Add($"{manifestPath} (manifest has no entries)");
                    continue;
                }

                var recovered = true;
                var unrecoveredTargets = new List<string>();
                foreach (var entry in manifest.Entries.AsEnumerable().Reverse())
                {
                    if (!IsPathUnderRoot(moduleRoot, entry.TargetPath) ||
                        !IsPathUnderRoot(moduleRoot, entry.TemporaryPath) ||
                        !IsPathUnderRoot(moduleRoot, entry.BackupPath))
                    {
                        recovered = false;
                        unrecoveredTargets.Add(entry.TargetPath);
                        continue;
                    }

                    try
                    {
                        if (entry.HadOriginal)
                        {
                            if (File.Exists(entry.BackupPath))
                            {
                                if (TargetExists(entry.TargetPath) &&
                                    !MatchesReplacement(
                                        entry.TargetPath,
                                        entry.ReplacementSha256))
                                {
                                    recovered = false;
                                    unrecoveredTargets.Add(
                                        $"{entry.TargetPath} (changed after the interrupted save)");
                                    continue;
                                }

                                File.Move(entry.BackupPath, entry.TargetPath, overwrite: true);
                                restored.Add(entry.TargetPath);
                            }
                            else if (!File.Exists(entry.TargetPath))
                            {
                                recovered = false;
                                unrecoveredTargets.Add(entry.TargetPath);
                            }
                            else if (MatchesReplacement(
                                         entry.TargetPath,
                                         entry.ReplacementSha256))
                            {
                                // The replacement landed, but the original backup is gone. Accepting
                                // this member as recovered would leave the group at mixed generations.
                                recovered = false;
                                unrecoveredTargets.Add(
                                    $"{entry.TargetPath} (original backup is missing)");
                                continue;
                            }
                        }
                        else if (TargetExists(entry.TargetPath))
                        {
                            if (!MatchesReplacement(
                                    entry.TargetPath,
                                    entry.ReplacementSha256))
                            {
                                recovered = false;
                                unrecoveredTargets.Add(
                                    $"{entry.TargetPath} (changed after the interrupted save)");
                                continue;
                            }

                            File.Delete(entry.TargetPath);
                        }

                        if (File.Exists(entry.TemporaryPath))
                            File.Delete(entry.TemporaryPath);
                    }
                    catch (Exception)
                    {
                        recovered = false;
                        unrecoveredTargets.Add(entry.TargetPath);
                    }
                }

                if (recovered)
                {
                    try
                    {
                        File.Delete(manifestPath);
                    }
                    catch (Exception)
                    {
                        recovered = false;
                    }
                }

                if (!recovered)
                {
                    foreach (var entry in manifest.Entries)
                        protectedBackups.Add(entry.BackupPath);

                    // A group whose entries could not all be put back leaves the ARE/GIT/GIC at mixed
                    // generations if opening proceeds anyway. Naming the specific files here - rather
                    // than only marking the transaction incomplete and moving on - is what lets
                    // WorkspaceContext.Open refuse to open instead of exposing the half-recovered group.
                    incompleteTransactions.Add(unrecoveredTargets.Count > 0
                        ? $"{manifestPath} (unrecovered: {string.Join(", ", unrecoveredTargets.Distinct())})"
                        : $"{manifestPath} (manifest could not be removed after recovery)");
                }
            }

            foreach (var backup in Directory.EnumerateFiles(
                         moduleRoot, "*" + BackupSuffix, SearchOption.AllDirectories))
            {
                if (protectedBackups.Contains(backup))
                    continue;

                // "area.git.json.<guid>.save-backup" -> "area.git.json"
                var withoutSuffix = backup[..^BackupSuffix.Length];

                // Backups belonging to an unreadable manifest's transaction stay untouched.
                var transactionSeparator = withoutSuffix.LastIndexOf('.');
                if (transactionSeparator >= 0 &&
                    protectedTransactionIds.Contains(withoutSuffix[(transactionSeparator + 1)..]))
                {
                    continue;
                }
                var target = Path.ChangeExtension(withoutSuffix, null);
                if (target.Length == 0)
                    continue;

                try
                {
                    // A backup beside a canonical file that exists is the tidy-up CommitAll did not
                    // get to. The save landed; only the leftover is stale.
                    if (File.Exists(target))
                    {
                        File.Delete(backup);
                        continue;
                    }

                    File.Move(backup, target);
                    restored.Add(target);
                }
                catch (Exception)
                {
                    // A backup that cannot be moved is left exactly where it is: it may be the only
                    // copy of the builder's work, and deleting it to tidy up would be the one
                    // unrecoverable move available here.
                }
            }

            // Refuse to report success when a group is still incomplete. WorkspaceContext.Open does
            // not catch this - it propagates to the same "failed to open" handling every other fatal
            // open error already uses - so the module stays unopened rather than exposing an area
            // whose ARE/GIT/GIC files are at mixed generations.
            if (incompleteTransactions.Count > 0)
                throw new SaveRecoveryException(incompleteTransactions);

            return restored;
        }

        /// <summary>
        /// Atomically creates a new file without replacing one that appeared after the caller's
        /// existence check. A failed staging write or losing the creation race leaves no partial target.
        /// </summary>
        public static void WriteNewAtomic(string path, byte[] bytes)
        {
            ModuleMutationLock.ThrowIfModuleLocked();
            using var moduleWriteLock = ModuleWriteLock.AcquireForResourcePath(path);

            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, bytes);
                File.Move(temporaryPath, path, overwrite: false);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        /// <summary>A serialized document written to its temporary file, waiting to replace the real one.</summary>
        public readonly record struct StagedWrite(
            string TargetPath,
            string TemporaryPath,
            bool MustNotExist = false);

        /// <summary>
        /// Writes <paramref name="bytes"/> to the temporary file beside <paramref name="path"/> without
        /// touching the real one yet.
        /// </summary>
        /// <remarks>
        /// Split out from <see cref="WriteAtomic"/> so a caller saving more than one file - an area is a
        /// .are and a .git, and they are one logical document - can get every serialization and every
        /// full write out of the way before any existing file is replaced. All the ways a save fails in
        /// practice (a locked file, a full disk, a document that will not serialize) then happen while
        /// nothing on disk has changed.
        /// </remarks>
        public static StagedWrite Stage(string path, byte[] bytes)
        {
            // Checked at staging as well as at commit: staging writes a real file into the folder
            // the packer is copying, and a stray .tmp arriving mid-pass is the thing the packer's
            // own filter exists to survive rather than something to hand it.
            ModuleMutationLock.ThrowIfModuleLocked();
            using var moduleWriteLock = ModuleWriteLock.AcquireForResourcePath(path);

            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllBytes(temporaryPath, bytes);
            return new StagedWrite(path, temporaryPath);
        }

        /// <summary>
        /// Stages a file that is the destination of a rename. Group commit will refuse rather than
        /// overwrite if another resource appears at the path after rename preflight.
        /// </summary>
        public static StagedWrite StageNew(string path, byte[] bytes)
        {
            var staged = Stage(path, bytes);
            return staged with { MustNotExist = true };
        }

        /// <summary>Replaces the real file with its staged content.</summary>
        public static void Commit(StagedWrite staged)
        {
            ModuleMutationLock.ThrowIfModuleLocked();
            using var moduleWriteLock =
                ModuleWriteLock.AcquireForResourcePath(staged.TargetPath);
            File.Move(staged.TemporaryPath, staged.TargetPath, overwrite: true);
        }

        /// <summary>
        /// Commits a group of staged writes as one logical save, restoring every original if any
        /// replacement fails.
        /// </summary>
        /// <remarks>
        /// The filesystem cannot atomically rename three files at once, so each existing target is moved
        /// to a transaction-unique backup before its staged replacement is installed. Backups remain
        /// available until every replacement succeeds. A locked later target therefore rolls earlier
        /// targets back instead of leaving an ARE/GIT/GIC triplet mixed on disk.
        /// </remarks>
        public static void CommitAll(IReadOnlyList<StagedWrite> stagedWrites)
        {
            ArgumentNullException.ThrowIfNull(stagedWrites);
            if (stagedWrites.Count == 0)
                return;

            // The whole group is refused up front. Half a triplet installed and then rolled back is
            // a worse outcome than not starting, and the pack is not going to finish mid-loop.
            ModuleMutationLock.ThrowIfModuleLocked();
            using var moduleWriteLock =
                ModuleWriteLock.AcquireForResourcePath(stagedWrites[0].TargetPath);

            var transactionId = Guid.NewGuid().ToString("N");
            var states = stagedWrites
                .Select(staged => new CommitState(staged, transactionId))
                .ToList();
            foreach (var state in states)
            {
                state.HadOriginal = File.Exists(state.Staged.TargetPath);
                if (state.HadOriginal && state.Staged.MustNotExist)
                {
                    foreach (var staged in stagedWrites)
                        Discard(staged);
                    throw new IOException(
                        $"A file already exists at rename destination '{state.Staged.TargetPath}'.");
                }
            }

            var manifestPath = TransactionManifestPath(states, transactionId);
            try
            {
                WriteTransactionManifest(manifestPath, states);

                foreach (var state in states)
                {
                    if (state.HadOriginal)
                    {
                        File.Move(state.Staged.TargetPath, state.BackupPath);
                        state.OriginalMoved = true;
                    }

                    File.Move(
                        state.Staged.TemporaryPath,
                        state.Staged.TargetPath,
                        overwrite: !state.Staged.MustNotExist);
                    state.ReplacementMoved = true;
                }

                // This is the commit point. While the manifest exists, startup always rolls the
                // entire group back. Once every replacement has landed, removing it declares the
                // new generation complete; any backups left by a later interruption are stale.
                File.Delete(manifestPath);
            }
            catch (Exception commitFailure)
            {
                var rollbackFailures = new List<Exception>();
                for (var i = states.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        Restore(states[i]);
                    }
                    catch (Exception rollbackFailure)
                    {
                        rollbackFailures.Add(rollbackFailure);
                    }
                }

                foreach (var staged in stagedWrites)
                    Discard(staged);

                if (rollbackFailures.Count > 0)
                {
                    rollbackFailures.Insert(0, commitFailure);
                    throw new AggregateException(
                        "The save failed and one or more original files could not be restored.",
                        rollbackFailures);
                }

                DeleteManifest(manifestPath);
                throw;
            }

            foreach (var state in states)
                DeleteBackup(state.BackupPath);
        }

        /// <summary>Throws away a staged write, leaving the real file untouched. Never throws.</summary>
        public static void Discard(StagedWrite staged)
        {
            try
            {
                File.Delete(staged.TemporaryPath);
            }
            catch (IOException)
            {
                // A leaked .tmp is untidy, not harmful - and never at the cost of masking the real
                // failure that got us here.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private sealed class CommitState
        {
            public CommitState(StagedWrite staged, string transactionId)
            {
                Staged = staged;
                BackupPath = staged.TargetPath + "." + transactionId + BackupSuffix;
            }

            public StagedWrite Staged { get; }

            /// <summary>
            /// Where the original waits while its replacement is installed. The GUID keeps two
            /// concurrent saves of the same file apart; <see cref="RecoverInterruptedSaves"/> reads
            /// the canonical path back out of it by dropping the GUID and this suffix.
            /// </summary>
            public string BackupPath { get; }
            public bool HadOriginal { get; set; }
            public bool OriginalMoved { get; set; }
            public bool ReplacementMoved { get; set; }
        }

        private static void Restore(CommitState state)
        {
            if (state.ReplacementMoved && File.Exists(state.Staged.TargetPath))
                File.Delete(state.Staged.TargetPath);

            if (state.HadOriginal && state.OriginalMoved && File.Exists(state.BackupPath))
                File.Move(state.BackupPath, state.Staged.TargetPath, overwrite: true);
        }

        private static void DeleteBackup(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // The save itself succeeded; a transaction-unique backup can be removed next launch.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string TransactionManifestPath(
            IReadOnlyList<CommitState> states,
            string transactionId)
        {
            var commonDirectory = Path.GetDirectoryName(
                Path.GetFullPath(states[0].Staged.TargetPath))!;

            while (states.Any(state =>
                       !IsPathUnderRoot(commonDirectory, state.Staged.TargetPath)))
            {
                commonDirectory = Directory.GetParent(commonDirectory)?.FullName
                    ?? throw new InvalidOperationException(
                        "Grouped save targets do not share a writable directory.");
            }

            return Path.Combine(commonDirectory, "." + transactionId + TransactionSuffix);
        }

        private static void WriteTransactionManifest(
            string manifestPath,
            IReadOnlyList<CommitState> states)
        {
            var manifest = new SaveTransactionManifest
            {
                Entries = states.Select(state => new SaveTransactionEntry
                {
                    TargetPath = Path.GetFullPath(state.Staged.TargetPath),
                    TemporaryPath = Path.GetFullPath(state.Staged.TemporaryPath),
                    BackupPath = Path.GetFullPath(state.BackupPath),
                    HadOriginal = state.HadOriginal,
                    ReplacementSha256 = ComputeSha256(state.Staged.TemporaryPath)
                }).ToList()
            };

            var temporaryPath = manifestPath + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(manifest));
                File.Move(temporaryPath, manifestPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static void DeleteManifest(string manifestPath)
        {
            try
            {
                File.Delete(manifestPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static bool IsPathUnderRoot(string root, string path)
        {
            var relative = Path.GetRelativePath(
                Path.GetFullPath(root),
                Path.GetFullPath(path));
            return !Path.IsPathRooted(relative) &&
                   relative != ".." &&
                   !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        }

        private static bool TargetExists(string path) =>
            File.Exists(path) || Directory.Exists(path);

        private static bool MatchesReplacement(string path, string? expectedSha256)
        {
            if (string.IsNullOrWhiteSpace(expectedSha256) || !File.Exists(path))
                return false;

            return string.Equals(
                ComputeSha256(path),
                expectedSha256,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }

        private sealed class SaveTransactionManifest
        {
            public List<SaveTransactionEntry> Entries { get; set; } = new();
        }

        private sealed class SaveTransactionEntry
        {
            public string TargetPath { get; set; } = string.Empty;
            public string TemporaryPath { get; set; } = string.Empty;
            public string BackupPath { get; set; } = string.Empty;
            public bool HadOriginal { get; set; }
            public string? ReplacementSha256 { get; set; }
        }
    }

}
