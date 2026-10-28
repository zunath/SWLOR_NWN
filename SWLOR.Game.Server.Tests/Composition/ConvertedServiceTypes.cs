namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>A converted service resolved to its types, with the parts it is split into (none for most services).</summary>
public sealed record ConvertedServiceTypes(
    string Name,
    string Domain,
    Type Facade,
    Type Contract,
    Type Instance,
    IReadOnlyList<ConvertedServicePartTypes> Parts);

/// <summary>A part of a split converted service: its interface and its class.</summary>
public sealed record ConvertedServicePartTypes(string Name, Type Contract, Type Instance);
