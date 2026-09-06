namespace SWLOR.Toolset.Domain.Editing
{
    /// <summary>
    /// A single reversible mutation memento against a JsonGffDocument's model objects. Apply and
    /// Revert restore exact prior/next state (raw bytes, not a re-formatted equivalent) so undo
    /// and redo round-trip a document byte-for-byte.
    /// </summary>
    public interface IDocumentEdit
    {
        /// <summary>Applies (or re-applies, on redo) this edit's "after" state.</summary>
        void Apply();

        /// <summary>Restores this edit's "before" state.</summary>
        void Revert();

        /// <summary>A short human-readable description for undo/redo menus and logging.</summary>
        string Describe();
    }

    internal interface IDocumentEditTargetProvider
    {
        IEnumerable<object> GetMutationTargets();
    }
}
