namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Whether a creature is hidden from other players, which the messaging reads. Foundation owns the interface and
    /// <c>IStealthService</c> extends it (supply rule 2 of MaintainabilityRefactorDomainOrder.md).
    /// </summary>
    public interface IConcealmentQuery
    {
        /// <summary>
        /// Returns whether the creature is hidden from other players: in stealth or invisible. Signals
        /// that would tell nearby players such a creature is there go only to the creature itself.
        /// </summary>
        bool IsConcealed(uint creature);
    }
}
