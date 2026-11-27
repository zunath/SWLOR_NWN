namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The summary of the ability impact a creature is resolving, as Character reads it (the status effects name the ability
    /// that applied them). Character owns the interface and the Combat-owned impact state store implements it (supply rule 2
    /// of MaintainabilityRefactorDomainOrder.md): the store takes no service, so the reader is built without a construction
    /// cycle. The read runs per status application and allocates nothing.
    /// </summary>
    public interface IActiveAbilityImpact
    {
        /// <summary>The summary of the activator's impact in progress, or null when it resolves none.</summary>
        AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator);
    }
}
