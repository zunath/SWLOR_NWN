using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Who each enemy hates and why: the enmity tables, the attack commands that follow the highest target, and the taunt, healing and damage rules that feed them.</summary>
    public interface IEnmityService
    {
        /// <summary>
        /// When an enemy is damaged, increase enmity toward that creature by the amount of damage dealt.
        /// </summary>
        void CreatureDamaged();

        int CalculateDamageEnmity(int damage, int adjustment);

        void HealingStarted();

        void HealingCompleted();

        int CalculateHealingEnmity(int requested, int previousHP, int currentHP, long nestedHealing = 0);

        /// <summary>
        /// When a creature attacks an enemy, increase enmity by 1.
        /// </summary>
        void CreatureAttacked();

        /// <summary>
        /// When a creature dies, remove all enmity tables it is associated with.
        /// </summary>
        void CreatureDeath();

        /// <summary>
        /// When a creature is destroyed with DestroyObject, remove all enmity tables it is associated with.
        /// </summary>
        void CreatureDestroyed();

        /// <summary>
        /// When a player dies, remove them from all enmity tables.
        /// </summary>
        void PlayerDeath();

        /// <summary>
        /// When a player leaves, remove them from all enmity tables.
        /// </summary>
        void PlayerExit();

        /// <summary>
        /// When a DM limbos creatures, ensure their enmity is wiped.
        /// </summary>
        void CreatureLimbo();

        /// <summary>
        /// Retrieves a table containing the creatures on a specific enemy's enmity table.
        /// If no creatures are on the enmity table, an empty dictionary will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to use for retrieval</param>
        /// <returns>A dictionary containing an enemy's enmity table.</returns>
        Dictionary<uint, int> GetEnmityTable(uint enemy);

        /// <summary>
        /// Retrieves the creature with the highest amount of enmity.
        /// If this cannot be determined, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to retrieve the highest target for.</param>
        /// <returns>The target with the highest enmity</returns>
        uint GetHighestEnmityTarget(uint enemy);

        /// <summary>
        /// Retrieves the creature with the highest amount of enmity that the enemy can attack.
        /// Creatures hidden from the enemy by invisibility or undetected stealth keep their enmity but are skipped until
        /// they can be seen again. If no creature can be attacked, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to retrieve the attack target for.</param>
        /// <returns>The visible target with the highest enmity</returns>
        uint GetHighestEnmityAttackTarget(uint enemy);

        /// <summary>
        /// Makes every enemy with this creature on its enmity table choose its attack target again.
        /// Call after the creature becomes hidden so enemies react without waiting for their next
        /// heartbeat or combat round.
        /// </summary>
        /// <param name="creature">The creature whose enemies should re-evaluate their target.</param>
        void ReevaluateEnemyAttackTargets(uint creature);

        /// <summary>
        /// Determines whether a creature holds the enemy's attention by a safe margin: it must be the
        /// enemy's highest enmity target and lead the next-highest creature by at least
        /// <see cref="SecureThreatLeadPercent"/>. Taunts are wasted when this is already true.
        /// </summary>
        /// <param name="enemy">The enemy whose enmity table is checked.</param>
        /// <param name="creature">The creature expected to hold the enemy's attention.</param>
        /// <returns>true if the creature's threat lead is secure, false otherwise.</returns>
        bool IsThreatSecured(uint enemy, uint creature);

        bool IsThreatLeadSecure(int holderEnmity, int nextHighestEnmity);

        /// <summary>
        /// Modifies the enmity of a specific target toward the specific creature.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The amount of enmity to adjust by</param>
        void ModifyEnmity(uint creature, uint enemy, int amount);

        int CalculateAdjustedEnmity(int amount, int adjustment);

        /// <summary>
        /// Catches up to an eligible rival plus a fixed, stat-adjusted lead. Repeated taunts
        /// while already ahead do not accumulate threat; there is no target lock.
        /// </summary>
        bool TryTaunt(uint creature, uint enemy, int bonus);

        int CalculateTauntEnmity(int current, int rival, int adjustedBonus);

        int ClampEnmityPercentAdjustment(int adjustment);

        /// <summary>
        /// Modifies the enmity of all creatures who have the specified creature on their enmity table.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="amount">The amount of enmity to adjust by.</param>
        void ModifyEnmityOnAll(uint creature, int amount);

        /// <summary>
        /// Reduces the creature's current enmity on every enemy table by a percentage.
        /// </summary>
        /// <param name="creature">The creature whose current enmity will be reduced.</param>
        /// <param name="percent">The percent of current enmity to remove.</param>
        void ReduceEnmityOnAll(uint creature, int percent);

        /// <summary>
        /// Reduces the creature's current enmity on a single enemy table by a percentage.
        /// </summary>
        /// <param name="creature">The creature whose current enmity will be reduced.</param>
        /// <param name="enemy">The enemy whose enmity table will be adjusted.</param>
        /// <param name="percent">The percent of current enmity to remove.</param>
        void ReduceEnmity(uint creature, uint enemy, int percent);

        int CalculateReducedEnmity(int current, int percent);

        /// <summary>
        /// Modifies enmity from aggro proximity and tracks the resulting contribution separately.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The proximity enmity amount to apply.</param>
        void ModifyProximityEnmity(uint creature, uint enemy, int amount);

        /// <summary>
        /// Removes a creature from all enmity tables.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        void RemoveCreatureEnmity(uint creature);

        /// <summary>
        /// Removes a creature from one enemy's enmity table without making the enemy pick a new target.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        /// <param name="enemy">The enemy whose enmity table is updated.</param>
        void RemoveEnmity(uint creature, uint enemy);

        /// <summary>
        /// Removes the tracked proximity enmity contribution from a specific enemy's table.
        /// </summary>
        /// <param name="creature">The creature to remove proximity enmity for.</param>
        /// <param name="enemy">The enemy whose enmity table should be updated.</param>
        /// <returns>true if proximity enmity was removed.</returns>
        bool RemoveProximityEnmity(uint creature, uint enemy);

        bool HasProximityEnmity(uint creature, uint enemy);

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

        /// <summary>
        /// Clears every creature from an enemy's enmity table.
        /// </summary>
        /// <param name="enemy">The enemy whose enmity table will be cleared.</param>
        void ClearEnmityTable(uint enemy);

        /// <summary>
        /// Determines if a creature has enmity towards any other creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>true if creature has enmity on any other creature, false otherwise</returns>
        bool HasEnmity(uint creature);

        /// <summary>
        /// Resumes existing combat after the caller clears the action queue. The previous attack
        /// command is no longer pending, so its throttle must not delay the replacement command.
        /// </summary>
        void ResumeAttackAfterActionsCleared(uint creature);

        /// <summary>
        /// Forces a creature to attack the highest enmity target it can see.
        /// Stops an existing chase when its last proximity target is no longer in range.
        /// Stops attacking, but keeps its enmity, while every target is hidden by invisibility or stealth.
        /// If creature did not have enmity, nothing will happen.
        /// If the creature is already actively attacking that target, nothing will happen.
        /// </summary>
        void AttackHighestEnmityTarget(uint creature);

        void IssueAttackCommand(uint creature, uint target, bool clearActions = true);

        /// <summary>
        /// Retrieves all of the enmity table information for a given creature.
        /// </summary>
        /// <param name="creature">The creature whose tables will be retrieved</param>
        /// <returns>A dictionary of enmity values for a given creature.</returns>
        Dictionary<uint, int> GetEnmityTowardsAllEnemies(uint creature);
    }
}
