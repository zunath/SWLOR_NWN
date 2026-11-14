namespace SWLOR.Architecture.Tests.Debt;

/// <summary>The size of one top-level type of the server, every partial declaration together.</summary>
/// <param name="Type">The full type name.</param>
/// <param name="Lines">The lines of its files (see <see cref="SizeScanner"/>), less the lines of exempt members and of exempt files; when every file is exempt, the lines of all of them.</param>
/// <param name="Files">The files that declare it and no exemption covers, sorted (empty when every file is exempt).</param>
/// <param name="IsLayout">True for a GUI window definition (a type that implements <c>IGuiWindowDefinition</c>): layout, measured by the layout method limit only.</param>
/// <param name="Exemption">The exemption that takes the whole type out of the size measure, or null.</param>
internal sealed record TypeSizeFact(string Type, int Lines, IReadOnlyList<string> Files, bool IsLayout, Exemption? Exemption);
