using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editing
{
    /// <summary>
    /// Binds a file path, its parsed <see cref="JsonGffDocument"/>, and an <see cref="UndoStack"/>
    /// together for one editing session. For the session's lifetime, guarded mutations anywhere
    /// (see <see cref="EditScope"/>) must happen inside a <see cref="DocumentTransaction"/> opened
    /// via <see cref="Begin"/>; dispose the session to lift that requirement.
    /// </summary>
    public sealed class DocumentSession : IDisposable
    {
        private readonly IDisposable _guard;
        private DateTime? _loadedMTimeUtc;
        private bool _disposed;

        public string FilePath { get; }

        public JsonGffDocument Document { get; }

        public UndoStack UndoStack { get; }

        /// <summary>Binds an already-parsed document to a path, recording the file's current mtime (if it exists) for HasExternalChange().</summary>
        public DocumentSession(string filePath, JsonGffDocument document)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            UndoStack = new UndoStack();
            _loadedMTimeUtc = File.Exists(filePath) ? File.GetLastWriteTimeUtc(filePath) : null;
            _guard = EditScope.EnterGuard();
        }

        /// <summary>Loads and parses the file at the given path into a new session.</summary>
        public static DocumentSession Open(string filePath)
        {
            return new DocumentSession(filePath, JsonGffDocument.Load(filePath));
        }

        /// <summary>Begins a transaction on this session's undo stack.</summary>
        public DocumentTransaction Begin(string description)
        {
            return UndoStack.Begin(description);
        }

        /// <summary>
        /// True if the file at FilePath has a different last-write time than when this session
        /// was opened (or has been deleted since, or now exists when it did not before).
        /// </summary>
        public bool HasExternalChange()
        {
            if (!File.Exists(FilePath))
                return _loadedMTimeUtc != null;

            return File.GetLastWriteTimeUtc(FilePath) != _loadedMTimeUtc;
        }

        /// <summary>
        /// Reloads the file into the existing document object, clears undo/redo history, and
        /// records the reloaded file state as the new external-change baseline.
        /// </summary>
        public void ReloadFromDisk()
        {
            Document.ReplaceWith(JsonGffDocument.Load(FilePath));
            UndoStack.Reset();
            RecordCurrentFileState();
        }

        /// <summary>Records the current on-disk state after this session successfully saves.</summary>
        public void RecordCurrentFileState()
        {
            _loadedMTimeUtc = File.Exists(FilePath) ? File.GetLastWriteTimeUtc(FilePath) : null;
        }

        /// <summary>Releases this session's guard on the ambient EditScope.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _guard.Dispose();
        }
    }
}
