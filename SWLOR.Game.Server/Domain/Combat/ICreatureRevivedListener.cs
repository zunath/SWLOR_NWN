namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Told when a creature is brought back where it fell (a subdual), so state the area of effect events keep can be applied
    /// again: those events do not fire for a creature that never left the area. Combat owns the interface and a higher service
    /// implements it (supply rule 2 of MaintainabilityRefactorDomainOrder.md); the death service takes every implementation,
    /// so an implementation must not need the death service.
    /// </summary>
    public interface ICreatureRevivedListener
    {
        /// <summary>The creature was revived in place.</summary>
        void OnCreatureRevived(uint creature);
    }
}
