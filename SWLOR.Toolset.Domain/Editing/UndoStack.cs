namespace SWLOR.Toolset.Domain.Editing
{
    /// <summary>
    /// Per-document undo/redo history of committed <see cref="DocumentTransaction"/>s, plus dirty
    /// tracking relative to a "saved" marker.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The history is a flat list with a "position" cursor: entries before the cursor have been
    /// applied, entries at/after it are available to redo. Pushing a new transaction after some
    /// undos truncates the discarded redo tail, matching standard editor undo/redo semantics.
    /// </para>
    /// <para>
    /// Dirty tracking compares the cursor position against a saved-position marker rather than
    /// hashing content: a freshly constructed stack starts clean (position 0 == saved marker 0).
    /// <see cref="MarkSaved"/> records the current position as clean. If a new transaction is
    /// pushed after undoing past the saved marker, the entries between the current position and
    /// the old end of history are discarded — if the saved marker pointed into that discarded
    /// range, it can never be reached again, so it is invalidated (set to "none") and the stack
    /// reports dirty unconditionally until the next <see cref="MarkSaved"/>.
    /// </para>
    /// </remarks>
    public sealed class UndoStack
    {
        private readonly List<IDocumentEdit> _entries = new();
        private int _position;
        private int? _savedPosition;

        public UndoStack()
        {
            _savedPosition = 0;
        }

        /// <summary>The committed undo steps, in application order.</summary>
        public IReadOnlyList<IDocumentEdit> Entries => _entries;

        /// <summary>Number of entries currently applied (0..Entries.Count).</summary>
        public int Position => _position;

        /// <summary>The most recently applied entry, used as an identity token for deferred work.</summary>
        public IDocumentEdit? CurrentAppliedEntry => _position > 0 ? _entries[_position - 1] : null;

        public bool CanUndo => _position > 0;

        public bool CanRedo => _position < _entries.Count;

        /// <summary>True when the current position differs from the last MarkSaved() position.</summary>
        public bool IsDirty => _savedPosition is not { } saved || saved != _position;

        /// <summary>
        /// Unwinds to the last saved position, or to the beginning when nothing has been saved.
        /// </summary>
        /// <remarks>
        /// Revert means "put this file back the way it is on disk", and every editor spelled it as
        /// <c>while (CanUndo) Undo()</c> - which is a different thing. Save does not clear the
        /// history, so after edit, save, edit, Revert that loop unwound the saved transaction too:
        /// the document ended up older than the version on disk and still marked dirty, and the
        /// next save wrote that over work the builder had already committed.
        ///
        /// The saved position can be on either side of the cursor. When it remains in the history,
        /// undo or redo back to it. Only an invalidated marker means branching discarded the saved
        /// state; in that case the beginning is the only defined fallback baseline.
        /// </remarks>
        public void RevertToSaved()
        {
            if (RestoreSaved())
                return;

            while (CanUndo)
                Undo();
        }

        /// <summary>Begins a new transaction that will push onto this stack when committed.</summary>
        public DocumentTransaction Begin(string description)
        {
            return new DocumentTransaction(this, description);
        }

        /// <summary>Called by DocumentTransaction.Commit() when it captured at least one edit.</summary>
        internal void Push(IDocumentEdit edit)
        {
            if (_savedPosition.HasValue && _savedPosition.Value > _position)
                _savedPosition = null;

            if (_position < _entries.Count)
                _entries.RemoveRange(_position, _entries.Count - _position);

            _entries.Add(edit);
            _position++;
        }

        /// <summary>
        /// Adds a continuation to an applied originating entry without changing the history cursor
        /// or discarding its redo tail. Deferred work can therefore remain part of the user action
        /// that caused it even after the builder undid a later unrelated edit.
        /// </summary>
        internal bool CoalesceIntoApplied(IDocumentEdit origin, IDocumentEdit continuation)
        {
            ArgumentNullException.ThrowIfNull(origin);
            ArgumentNullException.ThrowIfNull(continuation);

            var originIndex = -1;
            for (var index = 0; index < _position; index++)
            {
                if (MatchesOrigin(_entries[index], origin))
                {
                    originIndex = index;
                    break;
                }
            }

            if (originIndex < 0)
                return false;

            _entries[originIndex] = new CoalescedDocumentEdit(_entries[originIndex], continuation);
            if (_savedPosition.HasValue && _savedPosition.Value > originIndex)
            {
                // A saved state between the originating edit and its deferred continuation is no
                // longer representable by the coalesced history.
                _savedPosition = null;
            }

            return true;
        }

        internal bool ContainsApplied(IDocumentEdit origin)
        {
            ArgumentNullException.ThrowIfNull(origin);
            return _entries.Take(_position).Any(entry => MatchesOrigin(entry, origin));
        }

        private static bool MatchesOrigin(IDocumentEdit entry, IDocumentEdit origin)
        {
            return ReferenceEquals(entry, origin) ||
                   entry is CoalescedDocumentEdit coalesced && coalesced.Contains(origin);
        }

        public void Undo()
        {
            if (!CanUndo)
                throw new InvalidOperationException("Nothing to undo.");

            _position--;
            using (EditScope.EnterReplay())
                _entries[_position].Revert();
        }

        public void Redo()
        {
            if (!CanRedo)
                throw new InvalidOperationException("Nothing to redo.");

            using (EditScope.EnterReplay())
                _entries[_position].Apply();

            _position++;
        }

        /// <summary>
        /// Drops everything ahead of the current position, so nothing can be redone, while leaving the
        /// undo history intact.
        /// </summary>
        /// <remarks>
        /// <see cref="Push"/> already does this for the stack being edited. This exists for the case
        /// Push cannot see: an area is two documents with two stacks, and an edit to one of them has to
        /// invalidate the other's redo side as well. Without it, undoing an .are edit and then making a
        /// .git edit left the .are's redo entry live, and Ctrl+Y replayed the abandoned edit on top of
        /// the newer one - restoring content the builder had already discarded.
        /// </remarks>
        public void DiscardRedo()
        {
            if (_position >= _entries.Count)
                return;

            // Matches Push: a saved baseline that lives in the discarded tail can no longer be returned
            // to, so the document is dirty against a baseline that no longer exists.
            if (_savedPosition.HasValue && _savedPosition.Value > _position)
                _savedPosition = null;

            _entries.RemoveRange(_position, _entries.Count - _position);
        }

        /// <summary>Marks the current position as the clean/saved baseline.</summary>
        public void MarkSaved()
        {
            _savedPosition = _position;
        }

        /// <summary>
        /// Replays history in either direction until the last saved baseline is restored.
        /// Returns false when that baseline was discarded by branching after an undo.
        /// </summary>
        public bool RestoreSaved()
        {
            if (_savedPosition is not { } saved)
                return false;

            while (_position > saved)
                Undo();
            while (_position < saved)
                Redo();

            return true;
        }

        /// <summary>Clears all history and establishes the current document as a clean baseline.</summary>
        public void Reset()
        {
            _entries.Clear();
            _position = 0;
            _savedPosition = 0;
        }

        private sealed class CoalescedDocumentEdit : IDocumentEdit
        {
            private readonly IDocumentEdit _origin;
            private readonly IDocumentEdit _continuation;

            public CoalescedDocumentEdit(IDocumentEdit origin, IDocumentEdit continuation)
            {
                _origin = origin;
                _continuation = continuation;
            }

            public void Apply()
            {
                _origin.Apply();
                _continuation.Apply();
            }

            public void Revert()
            {
                _continuation.Revert();
                _origin.Revert();
            }

            public string Describe() => _origin.Describe();

            public bool Contains(IDocumentEdit origin)
            {
                return ReferenceEquals(_origin, origin) ||
                       _origin is CoalescedDocumentEdit coalesced && coalesced.Contains(origin);
            }
        }
    }
}
