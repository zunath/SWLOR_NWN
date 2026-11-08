namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What is left of combat once its parts are split out: the death, exit and six-second handlers, the cleanup that forgets
    /// a creature's combat state in every part, the refresh of the stat-driven trackers, and the damage roll that draws from the
    /// shared <c>Random</c>. The <c>Combat</c> facade forwards every other member straight to the part that owns it.
    /// </summary>
    public interface ICombatService
    {
        /// <summary>
        /// When a creature dies, applies the killer's and the recent damagers' defeated-enemy effects, then forgets the
        /// defeated creature's combat state.
        /// </summary>
        void ApplyDefeatedEnemyEffects();

        /// <summary>When a creature leaves the module, forgets its combat state.</summary>
        void ClearStatTriggerState();

        void RefreshStatDrivenTrackerEffects(uint creature);

        /// <summary>
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
        /// </summary>
        void ClearCombatState();

        /// <summary>
        /// Calculates a random damage amount based on the provided stats of the attacker and defender.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's raw defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <param name="deltaCap">Value to cap the lower and upper bounds of stat delta to. For weapons, should be weapon rank.</param>
        /// <returns>A damage value to apply to the target.</returns>
        int CalculateDamage(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0);
    }
}
