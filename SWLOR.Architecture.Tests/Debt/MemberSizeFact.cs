namespace SWLOR.Architecture.Tests.Debt;

/// <summary>The size of one method, constructor, operator, property, indexer or event of the server.</summary>
/// <param name="Key">The full type name, a dot and the member name; overloads add their parameter types (<c>Foo.Run(int)</c>).</param>
/// <param name="Type">The full name of the type that declares it (a nested type's own name).</param>
/// <param name="File">The file that declares it.</param>
/// <param name="Lines">The lines its declaration spans (signature, body, accessors and initializer).</param>
/// <param name="Limit">The limit that applies: the method limit, or the layout limit for a GUI window definition.</param>
/// <param name="Exemption">The exemption that takes it out of the measure, or null.</param>
internal sealed record MemberSizeFact(string Key, string Type, string File, int Lines, int Limit, Exemption? Exemption);
