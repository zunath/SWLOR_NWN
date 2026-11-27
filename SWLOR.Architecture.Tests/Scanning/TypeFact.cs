namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>A top-level type of the scanned assembly: what kind of type it is and which in-assembly interfaces it implements.</summary>
/// <param name="Key">Namespace and name, without the generic arity.</param>
/// <param name="IsInterface">True for an interface.</param>
/// <param name="IsStatic">True for a static class (abstract and sealed).</param>
/// <param name="IsSealed">True for a sealed class.</param>
/// <param name="Interfaces">The interfaces of the same assembly the type implements directly, as <c>Namespace.Name</c>.</param>
internal sealed record TypeFact(TypeKey Key, bool IsInterface, bool IsStatic, bool IsSealed, IReadOnlyList<string> Interfaces);
