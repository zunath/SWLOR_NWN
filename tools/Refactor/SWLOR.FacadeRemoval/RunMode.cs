namespace SWLOR.FacadeRemoval;

/// <summary>What a run of the tool does.</summary>
internal enum RunMode
{
    /// <summary>Rewrite the callers of every removed facade and rename its handlers in the manifest. Idempotent.</summary>
    Apply,

    /// <summary>Write nothing; fail when <see cref="Apply"/> would change something.</summary>
    Check,

    /// <summary>
    /// Undo the caller rewrites in one folder, so code written for the branch compiles against master (the parity
    /// copy of the characterization tests). Never run on the branch itself.
    /// </summary>
    Reverse
}
