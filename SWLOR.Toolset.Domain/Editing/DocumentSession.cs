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
        private static long _nextLockOrder;
        private readonly IDisposable _guard;
        private readonly object _syncRoot = new();
        private readonly long _lockOrder = Interlocked.Increment(ref _nextLockOrder);
        private DateTime? _loadedMTimeUtc;
        private byte[]? _loadedContentHash;
        private bool _disposed;

        public string FilePath { get; private set; }

        public JsonGffDocument Document { get; }

        public UndoStack UndoStack { get; }

        /// <summary>Binds an already-parsed document to a path, recording the file's current mtime (if it exists) for HasExternalChange().</summary>
        public DocumentSession(string filePath, JsonGffDocument document)
            : this(filePath, document, loadedContent: null)
        {
        }

        private DocumentSession(string filePath, JsonGffDocument document, byte[]? loadedContent)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            UndoStack = new UndoStack();
            var fileExists = File.Exists(filePath);
            _loadedMTimeUtc = fileExists
                ? File.GetLastWriteTimeUtc(filePath)
                : loadedContent != null
                    ? DateTime.MinValue
                    : null;
            _loadedContentHash = loadedContent != null
                ? System.Security.Cryptography.SHA256.HashData(loadedContent)
                : fileExists
                    ? System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(filePath))
                    : null;
            _guard = EditScope.EnterGuard();
        }

        /// <summary>Loads and parses the file at the given path into a new session.</summary>
        public static DocumentSession Open(string filePath)
        {
            var content = File.ReadAllBytes(filePath);
            return FromLoadedContent(filePath, JsonGffDocument.Parse(content), content);
        }

        /// <summary>
        /// Binds bytes and a document already parsed on a worker thread. The session itself is
        /// created on the caller's context so its ambient edit guard belongs to the editor, while
        /// the expensive file read and JSON parse stay off the UI thread.
        /// </summary>
        public static DocumentSession FromLoadedContent(
            string filePath,
            JsonGffDocument document,
            byte[] loadedContent)
        {
            ArgumentNullException.ThrowIfNull(loadedContent);
            return new DocumentSession(filePath, document, loadedContent);
        }

        /// <summary>Begins a transaction on this session's undo stack.</summary>
        public DocumentTransaction Begin(string description)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                return new DocumentTransaction(
                    UndoStack,
                    description,
                    new Releaser(() => Monitor.Exit(_syncRoot)));
            }
            catch
            {
                Monitor.Exit(_syncRoot);
                throw;
            }
        }

        /// <summary>
        /// Runs one grouped edit and commits it as a single undo step. If the mutation throws,
        /// every edit captured before the failure is rolled back and the exception is rethrown.
        /// </summary>
        public void Execute(string description, Action mutation)
        {
            ArgumentNullException.ThrowIfNull(mutation);

            using var transaction = Begin(description);
            try
            {
                mutation();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Runs deferred work and merges its captured edits into the originating applied edit.
        /// Returns false without mutating when that origin has left the applied history.
        /// </summary>
        public bool ExecuteCoalesced(
            IDocumentEdit origin,
            string description,
            Action mutation)
        {
            ArgumentNullException.ThrowIfNull(origin);
            ArgumentNullException.ThrowIfNull(mutation);

            lock (_syncRoot)
            {
                if (!UndoStack.ContainsApplied(origin))
                    return false;

                using var transaction = Begin(description);
                try
                {
                    mutation();
                    return transaction.CommitCoalescedInto(origin);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Applies derived metadata without adding an undo step. The caller must use this only for
        /// values fully determined by the document's authored content, such as a saved word count.
        /// </summary>
        public void ExecuteDerived(Action mutation)
        {
            ArgumentNullException.ThrowIfNull(mutation);

            lock (_syncRoot)
            {
                using (EditScope.EnterReplay())
                    mutation();
            }
        }

        /// <summary>
        /// True if the file at FilePath changed since this session loaded or last saved (deleted,
        /// newly created, different last-write time, or - because timestamp granularity can be
        /// coarse and external tools may preserve mtimes - different content under the same
        /// timestamp, decided by fingerprint).
        /// </summary>
        public bool HasExternalChange()
        {
            lock (_syncRoot)
            {
                if (!File.Exists(FilePath))
                    return _loadedMTimeUtc != null;

                if (File.GetLastWriteTimeUtc(FilePath) != _loadedMTimeUtc)
                    return true;

                if (_loadedContentHash == null)
                    return true;

                return !System.Security.Cryptography.SHA256
                    .HashData(File.ReadAllBytes(FilePath))
                    .AsSpan()
                    .SequenceEqual(_loadedContentHash);
            }
        }

        /// <summary>
        /// Captures the content fingerprint established when this session loaded or last saved,
        /// but only while the file still matches it. Multi-step destructive operations carry this
        /// immutable baseline through their final commit check instead of trusting an earlier
        /// external-change check.
        /// </summary>
        public bool TryCaptureUnchangedFileContentHash(out byte[] contentHash)
        {
            lock (_syncRoot)
            {
                contentHash = Array.Empty<byte>();
                if (_loadedMTimeUtc == null ||
                    _loadedContentHash == null ||
                    !File.Exists(FilePath) ||
                    File.GetLastWriteTimeUtc(FilePath) != _loadedMTimeUtc)
                {
                    return false;
                }

                var currentHash = System.Security.Cryptography.SHA256
                    .HashData(File.ReadAllBytes(FilePath));
                if (!currentHash.AsSpan().SequenceEqual(_loadedContentHash))
                    return false;

                contentHash = _loadedContentHash.ToArray();
                return true;
            }
        }

        /// <summary>
        /// Reloads the file into the existing document object, clears undo/redo history, and
        /// records the reloaded file state as the new external-change baseline.
        /// </summary>
        public void ReloadFromDisk()
        {
            var content = File.ReadAllBytes(FilePath);
            ReloadFrom(JsonGffDocument.Parse(content), content);
        }

        /// <summary>
        /// Replaces this session's content with an already-parsed reload. Callers reloading a
        /// multi-file group parse every member first, then commit them through this - so one
        /// malformed member cannot leave the group half-reloaded.
        /// </summary>
        public void ReloadFrom(JsonGffDocument document)
        {
            ReloadFrom(document, document.ToBytes());
        }

        /// <summary>
        /// Replaces this session's content and ties the baseline to the exact bytes that produced it.
        /// If the file changes after those bytes were read, the next external-change check sees the
        /// mismatch instead of accepting the newer disk generation as this document's baseline.
        /// </summary>
        public void ReloadFrom(JsonGffDocument document, byte[] loadedContent)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(loadedContent);
            lock (_syncRoot)
            {
                Document.ReplaceWith(document);
                UndoStack.Reset();
                RecordCurrentFileState(loadedContent);
            }
        }

        /// <summary>
        /// Accepts the current on-disk generation as a compare-and-swap baseline, such as after the
        /// user explicitly chooses Overwrite. Successful saves should use the byte[] overload so a
        /// replacement racing the post-save bookkeeping cannot be adopted accidentally.
        /// </summary>
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

        /// <summary>
        /// Records a successful save while keeping the baseline hash tied to the exact bytes written.
        /// The timestamp is still sampled from disk, but a replacement that lands before that sample
        /// cannot hide because its content will differ from this immutable hash.
        /// </summary>
        public void RecordCurrentFileState(byte[] savedContent)
        {
            ArgumentNullException.ThrowIfNull(savedContent);
            lock (_syncRoot)
            {
                _loadedMTimeUtc = File.Exists(FilePath)
                    ? File.GetLastWriteTimeUtc(FilePath)
                    : DateTime.MinValue;
                _loadedContentHash = System.Security.Cryptography.SHA256.HashData(savedContent);
            }
        }

        /// <summary>
        /// Rebinds this session to a new path after its file has been renamed on disk. The document
        /// and undo history carry over unchanged; only the identity and the external-change baseline
        /// move, so a save that renames stays one operation rather than a close-and-reopen that
        /// discards history.
        /// </summary>
        public void MoveTo(string newPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(newPath);

            lock (_syncRoot)
            {
                FilePath = newPath;
                _loadedMTimeUtc = File.Exists(newPath) ? File.GetLastWriteTimeUtc(newPath) : null;
            }
        }

        /// <summary>Serializes this document while excluding edits and undo/redo replay.</summary>
        public byte[] ToBytes()
        {
            lock (_syncRoot)
                return Document.ToBytes();
        }

        /// <summary>Undoes one transaction while excluding snapshot serialization.</summary>
        public void Undo()
        {
            lock (_syncRoot)
                UndoStack.Undo();
        }

        /// <summary>Redoes one transaction while excluding snapshot serialization.</summary>
        public void Redo()
        {
            lock (_syncRoot)
                UndoStack.Redo();
        }

        /// <summary>Restores the last saved undo position while holding the document lock.</summary>
        public bool RestoreSaved()
        {
            lock (_syncRoot)
                return UndoStack.RestoreSaved();
        }

        /// <summary>
        /// Unwinds every edit made since the last save - what an editor's Revert action means.
        /// </summary>
        /// <remarks>
        /// When the saved history position was discarded by branching (save, undo past it, then a
        /// new edit), the beginning of history is NOT the saved baseline - the disk is. Falling
        /// back to undo-everything let a following Save overwrite previously committed work with
        /// the initial load state, so the discarded-marker case reloads the on-disk document.
        /// </remarks>
        public void RevertToSaved()
        {
            lock (_syncRoot)
            {
                if (UndoStack.RestoreSaved())
                    return;

                ReloadFromDisk();
            }
        }

        /// <summary>
        /// Serializes several sessions under a stable lock order, producing a mutually consistent
        /// immutable snapshot without reading a live document graph on a worker thread.
        /// </summary>
        public static byte[][] CaptureSnapshots(params DocumentSession[] sessions)
        {
            ArgumentNullException.ThrowIfNull(sessions);
            if (sessions.Any(session => session == null))
                throw new ArgumentException("Snapshot sessions cannot contain null.", nameof(sessions));

            var ordered = sessions
                .Distinct()
                .OrderBy(session => session._lockOrder)
                .ToArray();
            byte[][]? snapshots = null;

            void CaptureUnderLock(int index)
            {
                if (index == ordered.Length)
                {
                    snapshots = sessions.Select(session => session.Document.ToBytes()).ToArray();
                    return;
                }

                lock (ordered[index]._syncRoot)
                    CaptureUnderLock(index + 1);
            }

            CaptureUnderLock(0);
            return snapshots!;
        }

        /// <summary>Releases this session's guard on the ambient EditScope.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _guard.Dispose();
        }

        private sealed class Releaser : IDisposable
        {
            private Action? _release;

            public Releaser(Action release)
            {
                _release = release;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _release, null)?.Invoke();
            }
        }
    }
}
