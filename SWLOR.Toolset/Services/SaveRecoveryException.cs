namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// Raised when <see cref="SaveService.RecoverInterruptedSaves"/> cannot fully restore an
    /// interrupted grouped save - for example because a canonical target or its backup is locked.
    /// Thrown rather than merely logged so the open call this runs under (see
    /// <see cref="Workspace.WorkspaceContext.Open"/>) refuses to proceed instead of exposing an
    /// area whose ARE/GIT/GIC files are left at mixed generations.
    /// </summary>
    public sealed class SaveRecoveryException(IReadOnlyList<string> incompleteTransactions)
        : IOException(
            "Interrupted save recovery is incomplete for: " +
            string.Join("; ", incompleteTransactions) +
            ". Resolve the lock or restore these files manually, then reopen the module.")
    {
        public IReadOnlyList<string> IncompleteTransactions { get; } = incompleteTransactions;
    }
}
