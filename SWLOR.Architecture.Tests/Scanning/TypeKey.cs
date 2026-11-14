namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>
/// A top-level type of the scanned assembly, named the way the source names it (no generic arity suffix).
/// Nested types and compiler-generated types (closures, lambdas, state machines) are attributed to the top-level
/// type that declares them, so a <see cref="TypeKey"/> is always the unit a developer would call "the class".
/// </summary>
internal readonly record struct TypeKey(string Namespace, string Name)
{
    public override string ToString() => Namespace.Length == 0 ? Name : Namespace + "." + Name;
}
