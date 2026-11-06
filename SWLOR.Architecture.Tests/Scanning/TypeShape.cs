using System.Reflection.Metadata;

namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>What a field type decodes to for the immutability check: a primitive, a named type, a generic instantiation, an array or something else.</summary>
/// <param name="Kind">The kind of type.</param>
/// <param name="Name">The full name without generic arity for a primitive or a type of another assembly; null for the rest.</param>
/// <param name="Definition">The type of the scanned assembly, when <paramref name="Kind"/> is <see cref="TypeShapeKind.Defined"/>.</param>
/// <param name="Arguments">The generic arguments of an instantiation.</param>
/// <param name="HasInitModifier">True when the type carries the <c>IsExternalInit</c> modifier (the return type of an init-only setter).</param>
internal sealed record TypeShape(
    TypeShapeKind Kind,
    string? Name = null,
    TypeDefinitionHandle Definition = default,
    IReadOnlyList<TypeShape>? Arguments = null,
    bool HasInitModifier = false)
{
    public IReadOnlyList<TypeShape> Args => Arguments ?? [];
}
