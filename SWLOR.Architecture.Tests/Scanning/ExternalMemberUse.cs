namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>A call or access of one named member of a type from another assembly, found in a method body.</summary>
/// <param name="Source">The top-level type that makes the call.</param>
/// <param name="ExternalType">The full name of the type that declares the member, without generic arity.</param>
/// <param name="MemberName">The member's metadata name: <c>add_ProcessExit</c> for an event subscription, <c>.ctor</c> for a constructor.</param>
/// <param name="Members">The members of <paramref name="Source"/> that make the call, as <c>Type::Member</c>.</param>
internal sealed record ExternalMemberUse(TypeKey Source, string ExternalType, string MemberName, IReadOnlySet<string> Members);
