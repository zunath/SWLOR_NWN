namespace SWLOR.FacadeRemoval;

/// <summary>
/// A member of a facade whose members went to more than one class (<c>Combat</c>'s fourteen parts, what is left of it on
/// <c>ICombatService</c>, and the pure <c>CombatRules</c>), and the class that holds it now. A caller of <c>Facade.Name</c> writes
/// <c>GameServices.Owner.Name</c> when <paramref name="Owner"/> is a <c>GameServices</c> property, or <c>Owner.Name</c> when it
/// is a class. A name listed under two owners (an overload on each) cannot be told apart from syntax, so the tool leaves
/// a call to it alone: it does not compile, and a person picks the overload.
/// </summary>
/// <param name="Name">The member's name, the same on the facade and on the owner.</param>
/// <param name="Owner">The name of a <c>GameServices</c> property (<c>CombatQueries</c>), or the full name of a class (<c>SWLOR.Game.Server.Domain.Combat.CombatRules</c>).</param>
internal sealed record RemovedMember(string Name, string Owner)
{
    /// <summary>True when <see cref="Owner"/> is a property of <c>GameServices</c>, not a class.</summary>
    public bool IsAccessorProperty => !Owner.Contains('.');

    /// <summary>The owner's simple name, which the rewritten caller writes.</summary>
    public string OwnerName => Owner[(Owner.LastIndexOf('.') + 1)..];

    /// <summary>The owner's namespace, which the rewritten caller imports; empty for a <c>GameServices</c> property.</summary>
    public string OwnerNamespace => Owner.LastIndexOf('.') < 0 ? string.Empty : Owner[..Owner.LastIndexOf('.')];
}
