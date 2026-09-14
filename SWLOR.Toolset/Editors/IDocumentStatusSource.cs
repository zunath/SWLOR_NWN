namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// An editor document that has something to add to the shell's status bar - the area editor
    /// contributes where the selection stands.
    /// </summary>
    /// <remarks>
    /// Coordinates belong here rather than in a field on the selection bar. Aurora put them in its status
    /// bar for the same reason: they change constantly, they are read rather than typed, and a box narrow
    /// enough to fit four of them in the chrome is too narrow to label.
    /// </remarks>
    public interface IDocumentStatusSource
    {
        /// <summary>Extra status text for the active document, or an empty string for none.</summary>
        string StatusDetail { get; }
    }
}
