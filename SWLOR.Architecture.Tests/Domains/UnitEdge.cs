namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// One unit referencing another unit that belongs to a different domain, with the members that hold the reference.
/// </summary>
internal sealed record UnitEdge(
    DependencyUnit From,
    DependencyUnit To,
    string FromDomain,
    string ToDomain,
    IReadOnlySet<string> Members)
{
    /// <summary>The identity of the edge in the baseline and in failure messages, for example <c>Combat -&gt; Stat</c>.</summary>
    public string Key => $"{From} -> {To}";

    /// <summary>For example <c>Combat -&gt; Character</c>.</summary>
    public string DomainPair => $"{FromDomain} -> {ToDomain}";

    /// <summary>Number of distinct referencing members; information only, never compared.</summary>
    public int MemberCount => Members.Count;
}
