namespace SWLOR.Toolset.Domain.Editing
{
    /// <summary>
    /// Ambient ("current ambient scope") ground the guarded mutation entry points on
    /// <c>JsonGffField</c> / <c>JsonGffStruct</c> call into, both to capture edits into whichever
    /// <see cref="DocumentTransaction"/> is currently open and to enforce that a document attached
    /// to an <see cref="UndoStack"/> (via a <see cref="DocumentSession"/>) is never mutated
    /// silently outside of one.
    /// </summary>
    /// <remarks>
    /// The three ambient counters below are <see cref="AsyncLocal{T}"/> so they flow correctly
    /// with async/await inside a single logical call, without leaking between unrelated
    /// concurrent call contexts (e.g. two NUnit tests running on different pool threads).
    /// <para>
    /// This scope is intentionally document-agnostic: field and struct model objects hold no
    /// back-reference to their owning <c>JsonGffDocument</c> (keeping the Gff layer minimal), so
    /// the "is this mutation guarded" question cannot be answered per-document. Instead, guarding
    /// is a single ambient depth counter incremented for the lifetime of every open
    /// <see cref="DocumentSession"/>. In the intended usage — one document actively open for
    /// editing per logical flow — this is equivalent to per-document guarding. Running multiple
    /// guarded sessions concurrently on the same call context (e.g. two documents open on one
    /// UI thread with interleaved edits) is out of scope for this transaction guard: mutating either document
    /// outside a transaction would throw as long as any session is open.
    /// </para>
    /// </remarks>
    public static class EditScope
    {
        private static readonly AsyncLocal<DocumentTransaction?> _currentTransaction = new();
        private static readonly AsyncLocal<int> _guardDepth = new();
        private static readonly AsyncLocal<int> _replayDepth = new();

        /// <summary>True while a DocumentTransaction is open on this call context.</summary>
        public static bool IsTransactionOpen => _currentTransaction.Value != null;

        /// <summary>Entered for the lifetime of a DocumentSession; see remarks on the type.</summary>
        internal static IDisposable EnterGuard()
        {
            _guardDepth.Value += 1;
            return new Releaser(() => _guardDepth.Value -= 1);
        }

        /// <summary>
        /// Suppresses the guard and edit capture for code that BUILDS a brand-new document rather than
        /// editing an open one.
        /// </summary>
        /// <remarks>
        /// Needed because the guard is ambient per call context rather than per document (see the remarks
        /// on this type): with an editor open, its <see cref="DocumentSession"/> has raised the depth for
        /// the whole UI context, so constructing an unrelated document on that context throws even though
        /// nothing that construction touches is on anyone's undo stack. That is not a theoretical case -
        /// it is what stopped the base-game palettes from loading at all once any editor was open.
        /// <para>
        /// Capture is suppressed as well as the guard. A freshly built document's fields are not edits to
        /// the document a surrounding transaction is recording, so letting them accumulate there would put
        /// another document's construction on that document's undo stack.
        /// </para>
        /// <para>
        /// This is for construction only. It must never wrap a mutation of a document a session owns -
        /// that is precisely what the guard exists to catch.
        /// </para>
        /// </remarks>
        public static IDisposable EnterConstruction()
        {
            var guard = _guardDepth.Value;
            var transaction = _currentTransaction.Value;
            _guardDepth.Value = 0;
            _currentTransaction.Value = null;

            return new Releaser(() =>
            {
                _guardDepth.Value = guard;
                _currentTransaction.Value = transaction;
            });
        }

        /// <summary>Entered for the lifetime of an open DocumentTransaction. Throws on nesting.</summary>
        internal static IDisposable EnterTransaction(DocumentTransaction transaction)
        {
            if (_currentTransaction.Value != null)
                throw new InvalidOperationException(
                    "A document transaction is already open on this call context; nested transactions are not supported. " +
                    "Commit or dispose the current transaction before beginning another.");

            _currentTransaction.Value = transaction;
            return new Releaser(() => _currentTransaction.Value = null);
        }

        /// <summary>
        /// Entered by UndoStack while replaying a previously captured edit (Undo/Redo). Mutation
        /// entry points still run their normal logic during replay, but guard enforcement and
        /// re-capture into a transaction are both suppressed so replay never throws and never
        /// records itself as a new edit.
        /// </summary>
        internal static IDisposable EnterReplay()
        {
            _replayDepth.Value += 1;
            return new Releaser(() => _replayDepth.Value -= 1);
        }

        /// <summary>
        /// Called by a guarded mutation entry point before it mutates model state. Allowed
        /// unconditionally during replay or while a transaction is open (the mutation will be
        /// captured below); otherwise throws if a DocumentSession's undo stack is attached so the
        /// mutation cannot happen outside a transaction. With no session ever attached, this is
        /// always a no-op and mutations remain unrestricted.
        /// </summary>
        internal static void EnsureMutationAllowed()
        {
            if (_replayDepth.Value > 0)
                return;

            if (_currentTransaction.Value != null)
                return;

            if (_guardDepth.Value > 0)
                throw new InvalidOperationException(
                    "This document is attached to an undo stack (a DocumentSession is open); " +
                    "mutate it inside a DocumentTransaction, e.g. using (var tx = session.Begin(\"description\")) { ... }.");
        }

        /// <summary>
        /// Called by a guarded mutation entry point after it mutates model state, with a memento
        /// describing exactly what changed. Appends to the currently open transaction, if any; a
        /// no-op during replay (the memento already exists on the stack) or when no transaction
        /// is open.
        /// </summary>
        internal static void Capture(IDocumentEdit edit)
        {
            if (_replayDepth.Value > 0)
                return;

            _currentTransaction.Value?.AddEdit(edit);
        }

        private sealed class Releaser : IDisposable
        {
            private Action? _dispose;

            public Releaser(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }
}
