namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>
    /// A script file bound to a path, with the external-change baseline the editor needs.
    /// The text-editing counterpart to <see cref="Editing.DocumentSession"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately does NOT carry an undo stack. <c>DocumentSession</c>'s stack models
    /// transactional edits over a GFF field tree; text is a different shape, and AvaloniaEdit's
    /// <c>TextDocument.UndoStack</c> already does the right thing for a character buffer. Trying to
    /// reuse the GFF stack here would mean re-implementing text undo badly. Dirty state is likewise
    /// derived by comparing against the last saved text rather than tracked as a flag, so an edit
    /// that is typed and then undone correctly reports clean.
    /// </remarks>
    public sealed class ScriptSession
    {
        private readonly object _syncRoot = new();
        private DateTime? _loadedMTimeUtc;
        private byte[]? _loadedContentHash;
        private string _savedText;

        private ScriptSession(string filePath, ScriptTextDocument document)
        {
            FilePath = filePath;
            Document = document;
            _savedText = document.Text;
            RecordCurrentFileState();
        }

        public string FilePath { get; }

        /// <summary>The on-disk shape (EOL/BOM/trailing newline) this session preserves.</summary>
        public ScriptTextDocument Document { get; private set; }

        /// <summary>The text as last loaded or saved. Compare against the buffer to derive dirtiness.</summary>
        public string SavedText
        {
            get { lock (_syncRoot) return _savedText; }
        }

        public static ScriptSession Open(string filePath) =>
            new(filePath, ScriptTextDocument.Load(filePath));

        /// <summary>Binds already-parsed content to a path. Used by tests and by in-memory callers.</summary>
        public static ScriptSession FromDocument(string filePath, ScriptTextDocument document) =>
            new(filePath, document);

        /// <summary>True when <paramref name="currentText"/> differs from what is on disk.</summary>
        public bool IsDirty(string currentText)
        {
            lock (_syncRoot)
                return !string.Equals(NormaliseForCompare(currentText), NormaliseForCompare(_savedText), StringComparison.Ordinal);
        }

        /// <summary>
        /// True if the file changed on disk since this session loaded or last saved (or was deleted,
        /// or now exists when it did not). Mirrors DocumentSession.HasExternalChange.
        /// </summary>
        public bool HasExternalChange()
        {
            lock (_syncRoot)
            {
                if (!File.Exists(FilePath))
                    return _loadedMTimeUtc != null;

                if (File.GetLastWriteTimeUtc(FilePath) != _loadedMTimeUtc)
                    return true;

                // Same mtime can hide a swap on coarse-granularity filesystems or under tools
                // that preserve timestamps; the content fingerprint decides.
                if (_loadedContentHash == null)
                    return true;

                return !System.Security.Cryptography.SHA256
                    .HashData(File.ReadAllBytes(FilePath))
                    .AsSpan()
                    .SequenceEqual(_loadedContentHash);
            }
        }

        /// <summary>Rereads the file, replacing the captured shape and the saved-text baseline.</summary>
        public ScriptTextDocument ReloadFromDisk()
        {
            lock (_syncRoot)
            {
                Document = ScriptTextDocument.Load(FilePath);
                _savedText = Document.Text;
                RecordCurrentFileState();
                return Document;
            }
        }

        /// <summary>Serialises <paramref name="text"/> through the captured on-disk shape.</summary>
        public byte[] ToBytes(string text)
        {
            lock (_syncRoot)
                return Document.ToBytes(text);
        }

        /// <summary>
        /// Marks <paramref name="text"/> as the saved baseline after successfully writing
        /// <paramref name="savedBytes"/>.
        /// </summary>
        /// <remarks>
        /// The content fingerprint must come from the immutable bytes accepted by the atomic save,
        /// not from rereading the path after its module lease has been released. An external writer
        /// can replace the file in that gap; retaining our hash makes that replacement visible to
        /// <see cref="HasExternalChange"/> instead of accidentally adopting it as the new baseline.
        /// </remarks>
        public void MarkSaved(string text, byte[] savedBytes)
        {
            ArgumentNullException.ThrowIfNull(savedBytes);

            lock (_syncRoot)
            {
                _savedText = NormaliseForCompare(text);
                // A successful atomic save established an existing-file generation. Keep that
                // fact non-null even if another writer deletes the path before this bookkeeping.
                _loadedMTimeUtc = File.Exists(FilePath)
                    ? File.GetLastWriteTimeUtc(FilePath)
                    : DateTime.MinValue;
                _loadedContentHash = System.Security.Cryptography.SHA256.HashData(savedBytes);
            }
        }

        public void RecordCurrentFileState()
        {
            lock (_syncRoot)
            {
                _loadedMTimeUtc = File.Exists(FilePath) ? File.GetLastWriteTimeUtc(FilePath) : null;
                _loadedContentHash = _loadedMTimeUtc != null
                    ? System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(FilePath))
                    : null;
            }
        }

        // The buffer always hands back '\n'; the baseline came off disk the same way. Normalising
        // both sides anyway keeps a caller that passes raw CRLF text from reading as permanently dirty.
        private static string NormaliseForCompare(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');
    }
}
