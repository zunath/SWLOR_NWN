namespace SWLOR.Architecture.Tests.Debt;

/// <summary>One reviewed exemption from the size and services measures (targets 2 and 3, owner decision 1).</summary>
/// <param name="Match">What <paramref name="Value"/> names: <c>folder</c> (a path prefix ending in <c>/</c>), <c>file</c>, <c>type</c> (a full type name), <c>member</c> (a full type name, a dot and a member name) or <c>generatedHeader</c> (text in a file's header, before its first declaration).</param>
/// <param name="Value">The folder, file, type, member or header text.</param>
/// <param name="Measures">The measures it exempts from: <c>typeSize</c>, <c>methodSize</c>, <c>servicesPerType</c>.</param>
/// <param name="Category">The approved category: generated code, migration, attribute-row enum, content data, data catalog member, fluent builder.</param>
/// <param name="Reason">Why this entry is in the category.</param>
internal sealed record Exemption(string Match, string Value, IReadOnlyList<string> Measures, string Category, string Reason)
{
    public override string ToString() => $"{Match} {Value}";
}
