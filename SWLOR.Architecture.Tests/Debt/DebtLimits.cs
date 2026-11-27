namespace SWLOR.Architecture.Tests.Debt;

/// <summary>The limits the owner approved on 2026-11-14 (targets 2 and 3).</summary>
internal static class DebtLimits
{
    /// <summary>A logic type is at most this many lines, every partial declaration together.</summary>
    public const int TypeLines = 1000;

    /// <summary>A logic method (or property, accessor, operator) is at most this many lines.</summary>
    public const int MethodLines = 150;

    /// <summary>A GUI layout method (a member of a GUI window definition) is at most this many lines.</summary>
    public const int LayoutMethodLines = 400;

    /// <summary>A type uses at most this many services, through its constructor or a lookup that remains.</summary>
    public const int ServicesPerType = 10;
}
