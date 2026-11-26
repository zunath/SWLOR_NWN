namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The refresh of the trackers that follow a creature's stats, which a perk refund asks for. Character owns the interface and
    /// <c>ICombatService</c> extends it (supply rule 2 of MaintainabilityRefactorDomainOrder.md), so the perk refund part needs
    /// nothing from the combat service.
    /// </summary>
    public interface IStatDrivenTrackerRefresh
    {
        void RefreshStatDrivenTrackerEffects(uint creature);
    }
}
