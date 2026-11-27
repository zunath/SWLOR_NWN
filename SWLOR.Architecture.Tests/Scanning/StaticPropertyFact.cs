namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>A static property of a type written in the source, with the type it returns.</summary>
/// <param name="Owner">The top-level type that declares the property, or that declares the nested type that does.</param>
/// <param name="Name">The property name.</param>
/// <param name="Type">The full name of the property's type (<c>Namespace.Name</c>, without generic arity), or empty when it is not a named type.</param>
internal sealed record StaticPropertyFact(TypeKey Owner, string Name, string Type);
