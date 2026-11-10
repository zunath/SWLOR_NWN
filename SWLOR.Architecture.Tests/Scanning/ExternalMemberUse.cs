namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>A call or read of one member of a type from another assembly, found in a method body.</summary>
/// <param name="Source">The top-level type that makes the use.</param>
/// <param name="ExternalType">The type that declares the member, <c>Namespace.Name</c>.</param>
/// <param name="MemberName">The member's metadata name (<c>GetAssemblies</c>, <c>get_All</c>).</param>
/// <param name="Members">The members of <paramref name="Source"/> that make the use, as <c>Type::Member</c>.</param>
internal sealed record ExternalMemberUse(TypeKey Source, string ExternalType, string MemberName, IReadOnlySet<string> Members);
