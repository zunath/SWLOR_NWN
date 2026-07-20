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

        public bool CanUndo => _position > 0;

        public bool CanRedo => _position < _entries.Count;

        /// <summary>True when the current position differs from the last MarkSaved() position.</summary>
        public bool IsDirty => _savedPosition is not { } saved || saved != _position;

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

        /// <summary>Marks the current position as the clean/saved baseline.</summary>
        public void MarkSaved()
        {
            _savedPosition = _position;
        }
    }
}
