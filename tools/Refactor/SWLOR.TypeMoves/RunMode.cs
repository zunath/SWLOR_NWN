namespace SWLOR.TypeMoves;

/// <summary>What a run of the tool does.</summary>
internal enum RunMode
{
    /// <summary>Move the types and rewrite their references. Idempotent.</summary>
    Apply,

    /// <summary>Write nothing; fail when <see cref="Apply"/> would change something.</summary>
    Check
}
