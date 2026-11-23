namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What stealth detection sets off in the creature AI. Combat owns the interface; the creature AI implements it and
    /// registers itself with <see cref="IStealthService.RegisterAggroListener"/> at boot, so stealth notifies the AI
    /// without a construction cycle (the AI reads stealth for its aggro checks).
    /// </summary>
    public interface IStealthAggroListener
    {
        /// <summary>
        /// Re-enters the normal proximity-aggro path when a later native Spot check reveals a
        /// stealthed player who is still inside the observer's aggro aura. Non-AI observers and
        /// targets that no longer satisfy the ordinary aggro guards are ignored.
        /// </summary>
        void TryAcquireAggroAfterDetection(uint observer, uint target);

        /// <summary>
        /// Runs the normal proximity-aggro path for every enemy within aggro range of a creature
        /// that just became visible. Their aggro auras only check on entry, so enemies the creature
        /// reached while hidden would otherwise ignore it until it left the aura and came back.
        /// </summary>
        void AcquireAggroFromNearbyEnemies(uint target);
    }
}
