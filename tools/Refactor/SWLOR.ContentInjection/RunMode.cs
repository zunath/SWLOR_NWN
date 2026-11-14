namespace SWLOR.ContentInjection;

/// <summary>What a run of the tool does.</summary>
internal enum RunMode
{
    /// <summary>Convert the listed content files, rewrite creation sites and callers, regenerate the registrations. Idempotent.</summary>
    Apply,

    /// <summary>Write nothing; fail when <see cref="Apply"/> would change something.</summary>
    Check,

    /// <summary>
    /// Turn converted text in one folder back into master's form (creation sites back to <c>new</c>, fixture properties removed), so
    /// the branch's tests compile against master. Runs on any tree: the parity copy of master has no <c>Core/IContentFactory.cs</c>.
    /// </summary>
    Reverse
}
