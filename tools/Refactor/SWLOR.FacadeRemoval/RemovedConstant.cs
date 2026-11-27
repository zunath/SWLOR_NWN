namespace SWLOR.FacadeRemoval;

/// <summary>
/// A public constant of a removed facade and the class that now holds it (the class the facade's initializer named,
/// such as <c>SWLOR.Game.Server.Domain.Combat.CombatRules</c>). Callers read <c>Owner.Name</c> instead of <c>Facade.Name</c>.
/// </summary>
/// <param name="Name">The constant's name, the same on the facade and on the owner.</param>
/// <param name="Owner">The full name of the owner type.</param>
internal sealed record RemovedConstant(string Name, string Owner)
{
    /// <summary>The owner's simple name, which the rewritten caller writes.</summary>
    public string OwnerName => Owner[(Owner.LastIndexOf('.') + 1)..];

    /// <summary>The owner's namespace, which the rewritten caller imports.</summary>
    public string OwnerNamespace => Owner.LastIndexOf('.') < 0 ? string.Empty : Owner[..Owner.LastIndexOf('.')];
}
