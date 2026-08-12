namespace SWLOR.Toolset.Domain.Editing
{
    /// <summary>
    /// Groups every guarded mutation made while it is open into a single undo step. Begin one via
    /// <see cref="UndoStack.Begin"/> or <see cref="DocumentSession.Begin"/>; dispose (or call
    /// <see cref="Commit"/>) to close it. Nesting is not supported — beginning a second
    /// transaction on the same call context while one is already open throws (see
    /// <see cref="EditScope.EnterTransaction"/>); group unrelated edits under one description
    /// instead of nesting.
    /// </summary>
    public sealed class DocumentTransaction : IDocumentEdit, IDocumentEditTargetProvider, IDisposable
    {
        private readonly UndoStack _stack;
        private readonly List<IDocumentEdit> _edits = new();
        private readonly IDisposable _scope;
        private readonly IDisposable? _ownerLock;
        private bool _finished;

        /// <summary>The description this transaction will be shown under in undo/redo UI.</summary>
        public string Description { get; }

        /// <summary>The edits captured so far, in the order they were applied.</summary>
        public IReadOnlyList<IDocumentEdit> Edits => _edits;

        internal DocumentTransaction(UndoStack stack, string description, IDisposable? ownerLock = null)
        {
            _stack = stack ?? throw new ArgumentNullException(nameof(stack));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            _ownerLock = ownerLock;
            _scope = EditScope.EnterTransaction(this);
        }

        /// <summary>Called by EditScope.Capture while this transaction is the open one.</summary>
        internal void AddEdit(IDocumentEdit edit)
        {
            if (_finished)
                throw new InvalidOperationException("Cannot record an edit into a transaction that has already finished.");

            _edits.Add(edit);
        }

        /// <summary>
        /// Ends recording. If any edits were captured, pushes this transaction onto the owning
        /// UndoStack as a single undo step; an empty transaction (no mutations occurred) is
        /// silently discarded. Safe to call more than once.
        /// </summary>
        public void Commit()
        {
            if (_finished)
                return;

            _finished = true;
            _scope.Dispose();
            try
            {
                if (_edits.Count > 0)
                    _stack.Push(this);
            }
            finally
            {
                _ownerLock?.Dispose();
            }
        }

        /// <summary>
        /// Ends recording and attaches the captured edits to an earlier applied action without
        /// pushing a new history entry. If the origin is no longer applied, the captured mutations
        /// are rolled back and false is returned.
        /// </summary>
        internal bool CommitCoalescedInto(IDocumentEdit origin)
        {
            if (_finished)
                return false;

            _finished = true;
            _scope.Dispose();
            try
            {
                if (_edits.Count == 0)
                    return true;
                if (_stack.CoalesceIntoApplied(origin, this))
                    return true;

                using (EditScope.EnterReplay())
                    Revert();
                return false;
            }
            finally
            {
                _ownerLock?.Dispose();
            }
        }

        /// <summary>
        /// Ends recording without adding an undo step and reverts every mutation captured so far.
        /// This is the exception path for multi-field edits: callers can abandon a partially
        /// applied mutation without leaving the document changed or dirty. Safe to call more than
        /// once.
        /// </summary>
        public void Rollback()
        {
            if (_finished)
                return;

            _finished = true;
            _scope.Dispose();
            try
            {
                using (EditScope.EnterReplay())
                    Revert();
            }
            finally
            {
                _ownerLock?.Dispose();
            }
        }

        /// <summary>Equivalent to Commit(); lets transactions be opened in a using block.</summary>
        public void Dispose()
        {
            Commit();
        }

        public void Apply()
        {
            foreach (var edit in _edits)
                edit.Apply();
        }

        public void Revert()
        {
            for (var i = _edits.Count - 1; i >= 0; i--)
                _edits[i].Revert();
        }

        public string Describe()
        {
            return Description;
        }

        IEnumerable<object> IDocumentEditTargetProvider.GetMutationTargets()
        {
            return _edits.SelectMany(UndoStack.GetMutationTargets);
        }
    }
}
