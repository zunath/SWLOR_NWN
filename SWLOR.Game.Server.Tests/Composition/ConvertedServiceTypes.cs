namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// One class of a converted service resolved to its types: the main one, or an extra (a further static facade or a part).
/// A part has no static facade, so <see cref="Facade"/> is null for it.
/// </summary>
public sealed record ConvertedServiceTypes(string Name, string Domain, Type? Facade, Type Contract, Type Instance);
