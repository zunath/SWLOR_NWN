namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The enmity reads Espionage infiltration makes. Character owns the interface and <c>IEnmityService</c> extends it
    /// (supply rule 2 of MaintainabilityRefactorDomainOrder.md; P5-15b).
    /// </summary>
    public interface IEspionageEnmityQuery
    {
        /// <summary>
        /// Returns true when a specific creature/enemy pair has enmity beyond the amount created
        /// solely by the enemy's aggro aura.
        /// </summary>
        bool HasNonProximityEnmity(uint creature, uint enemy);

        /// <summary>
        /// Returns true when an enemy has any enmity that did not come solely from its aggro aura.
        /// Attack, damage, and ability enmity make the enemy an active combatant and therefore an
        /// invalid source for a new Espionage infiltration attempt.
        /// </summary>
        bool HasNonProximityEnmity(uint enemy);

        /// <summary>
        /// Returns true when a creature appears on any enemy table for more than aggro proximity.
        /// This distinguishes real combat from the proximity-only entries created as multiple
        /// stealthed players cross overlapping aggro auras.
        /// </summary>
        bool HasNonProximityEnmityForCreature(uint creature);

        /// <summary>
        /// Returns true when either member of a creature/enemy pair has combat enmity involving
        /// someone outside that pair. Pair-specific checks use this to distinguish an expected
        /// aggro transition from unrelated combat.
        /// </summary>
        bool HasNonProximityEnmityOutsidePair(uint first, uint second);
    }
}
