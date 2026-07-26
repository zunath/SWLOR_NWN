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
    }
}
