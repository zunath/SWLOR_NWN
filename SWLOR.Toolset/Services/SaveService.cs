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

        public static void WriteAtomic(string path, byte[] bytes)
        {
            var temporaryPath = path + ".tmp";
            File.WriteAllBytes(temporaryPath, bytes);
            File.Move(temporaryPath, path, overwrite: true);
        }
    }
}
