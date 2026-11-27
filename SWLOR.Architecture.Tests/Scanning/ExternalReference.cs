namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>A use of a type from another assembly, found in a method body or in a signature.</summary>
/// <param name="Source">The top-level type that makes the reference.</param>
/// <param name="ExternalType">The referenced type's full name, <c>Namespace.Name</c>, without generic arity.</param>
/// <param name="Members">The members of <paramref name="Source"/> that hold the reference, as <c>Type::Member</c>.</param>
internal sealed record ExternalReference(TypeKey Source, string ExternalType, IReadOnlySet<string> Members);
