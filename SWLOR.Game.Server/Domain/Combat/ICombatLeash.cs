namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What the enmity tables ask of the creature leash: start the evade when an enemy has been pulled too far from home, and
    /// stop a chase whose proximity threat has expired. Combat owns the interface and the creature AI implements it
    /// (<c>IAIService : ICombatLeash</c>), so Combat does not name a Creatures type.
    /// </summary>
    public interface ICombatLeash
    {
        bool TryStartCombatLeashEvade(uint creature, uint target);

        /// <summary>
        /// Stops a chase whose proximity threat has expired, using the normal return-home behavior.
        /// </summary>
        void StopCombatAfterProximityLoss(uint enemy);
    }
}
