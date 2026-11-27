using SWLOR.Architecture.Tests.Domains;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>One source member of the server naming a type of another unit (see <see cref="SourceReferenceScanner"/>).</summary>
/// <param name="File">The file of the member, relative to the repository root.</param>
/// <param name="From">The unit of the member's top-level type.</param>
/// <param name="Member">
/// The member: its top-level type's full name, the nested types down to it and the member name
/// (<c>SWLOR.Game.Server.Domain.Character.StatService::GetStatAdjustment</c>, <c>...Outer+Inner::Run</c>); overloads are one member,
/// and code outside every member (a base list, an attribute of the type) is <c>(type)</c>.
/// </param>
/// <param name="To">The unit of the named type's top-level type.</param>
/// <param name="Target">The named type, nested types included (<c>CombatRules</c>, <c>AbilityDetail</c>), for the report.</param>
internal sealed record SourceReference(string File, DependencyUnit From, string Member, DependencyUnit To, string Target);
