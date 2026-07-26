using SWLOR.Toolset.Domain.Editing;
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
                WriteAtomic(session.FilePath, session.ToBytes());
                session.UndoStack.MarkSaved();
                _log.AppendLine($"Saved {session.FilePath}.");
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {session.FilePath}: {ex.Message}");
                return false;
            }
        }

        public static void WriteAtomic(string path, byte[] bytes) => Commit(Stage(path, bytes));

        /// <summary>
        /// Atomically creates a new file without replacing one that appeared after the caller's
        /// existence check. A failed staging write or losing the creation race leaves no partial target.
        /// </summary>
        public static void WriteNewAtomic(string path, byte[] bytes)
        {
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
        public readonly record struct StagedWrite(string TargetPath, string TemporaryPath);

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
            var temporaryPath = path + ".tmp";
            File.WriteAllBytes(temporaryPath, bytes);
            return new StagedWrite(path, temporaryPath);
        }

        /// <summary>Replaces the real file with its staged content.</summary>
        public static void Commit(StagedWrite staged) =>
            File.Move(staged.TemporaryPath, staged.TargetPath, overwrite: true);

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

            var states = stagedWrites.Select(staged => new CommitState(staged)).ToList();
            try
            {
                foreach (var state in states)
                {
                    state.HadOriginal = File.Exists(state.Staged.TargetPath);
                    if (state.HadOriginal)
                    {
                        File.Move(state.Staged.TargetPath, state.BackupPath);
                        state.OriginalMoved = true;
                    }

                    File.Move(state.Staged.TemporaryPath, state.Staged.TargetPath, overwrite: true);
                    state.ReplacementMoved = true;
                }
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
            public CommitState(StagedWrite staged)
            {
                Staged = staged;
                BackupPath = staged.TargetPath + "." + Guid.NewGuid().ToString("N") + ".save-backup";
            }

            public StagedWrite Staged { get; }
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
    }
}
