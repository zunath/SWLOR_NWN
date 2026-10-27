namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>A static field of a type written in the source (compiler-generated types and fields are left out).</summary>
/// <param name="Owner">The top-level type that declares the field, or that declares the nested type that does.</param>
/// <param name="Declaring">The declaring type as written, <c>Outer+Nested</c> for a nested type.</param>
/// <param name="Name">The field name.</param>
/// <param name="IsLiteral">True for a <c>const</c>: a compile-time value that can never change.</param>
/// <param name="IsInitOnly">True for <c>static readonly</c>. The reference cannot change, but a collection behind it can.</param>
/// <param name="IsAutoProperty">True for the field the compiler adds behind a static auto-property; <paramref name="Name"/> is the property's name.</param>
internal sealed record StaticFieldFact(TypeKey Owner, string Declaring, string Name, bool IsLiteral, bool IsInitOnly, bool IsAutoProperty = false)
{
    public override string ToString() => $"{Owner.Namespace}.{Declaring}.{Name}" + (IsAutoProperty ? " (static auto-property)" : string.Empty);
}
