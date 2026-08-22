namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// What the shell needs from whichever editor document tab is active: its undo history and the
    /// ability to save itself. The File and Edit menus (and their Ctrl+S / Ctrl+Z / Ctrl+Y hotkeys)
    /// act on the active tab through this, so they work identically for blueprints and areas
    /// without the shell knowing which kind of editor is in front.
    /// </summary>
    /// <remarks>
    /// Implementors are also <c>INotifyPropertyChanged</c> documents; the shell re-evaluates
    /// <see cref="CanUndo"/> / <see cref="CanRedo"/> whenever they raise a property change.
    /// </remarks>
    public interface IEditorDocument
    {
        /// <summary>
        /// True while this document owns a long-running operation that must finish before another
        /// editor action can safely enter the module. Most documents never become busy.
        /// </summary>
        bool IsBusy => false;

        bool CanUndo { get; }

        bool CanRedo { get; }

        void Undo();

        void Redo();

        /// <summary>Saves this editor, returning false when the user cancels or the write fails.</summary>
        Task<bool> TrySaveAsync();
    }
}
