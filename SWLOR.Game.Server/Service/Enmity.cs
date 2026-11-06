using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Who each enemy hates and why: the enmity tables, the attack commands that follow the highest target, and the
    /// taunt, healing and damage rules that feed them. The logic lives in
    /// <see cref="IEnmityService"/>; the event handlers stay here and every member forwards to the container's instance.
    /// </summary>
    public static class Enmity
    {
        private static IEnmityService Instance => FacadeAccessor.Resolve<IEnmityService>();

        public const int MinimumEnmityPercentAdjustment = Domain.Combat.EnmityService.MinimumEnmityPercentAdjustment;
        public const int MaximumEnmityPercentAdjustment = Domain.Combat.EnmityService.MaximumEnmityPercentAdjustment;
        public const int SecureThreatLeadPercent = Domain.Combat.EnmityService.SecureThreatLeadPercent;

        /// <summary>
        /// When an enemy is damaged, increase enmity toward that creature by the amount of damage dealt.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDamagedBefore)]
        public static void CreatureDamaged() =>
            Instance.CreatureDamaged();

        public static int CalculateDamageEnmity(int damage, int adjustment) =>
            Instance.CalculateDamageEnmity(damage, adjustment);

        [NWNEventHandler(ScriptName.OnHealBefore)]
        public static void HealingStarted() =>
            Instance.HealingStarted();

        [NWNEventHandler(ScriptName.OnHealAfter)]
        public static void HealingCompleted() =>
            Instance.HealingCompleted();

        public static int CalculateHealingEnmity(int requested, int previousHP, int currentHP, long nestedHealing = 0) =>
            Instance.CalculateHealingEnmity(requested, previousHP, currentHP, nestedHealing);

        /// <summary>
        /// When a creature attacks an enemy, increase enmity by 1.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAttackBefore)]
        public static void CreatureAttacked() =>
            Instance.CreatureAttacked();

        /// <summary>
        /// When a creature dies, remove all enmity tables it is associated with.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void CreatureDeath() =>
            Instance.CreatureDeath();

        /// <summary>
        /// When a creature is destroyed with DestroyObject, remove all enmity tables it is associated with.
        /// </summary>
        [NWNEventHandler(ScriptName.OnObjectDestroyed)]
        public static void CreatureDestroyed() =>
            Instance.CreatureDestroyed();

        /// <summary>
        /// When a player dies, remove them from all enmity tables.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void PlayerDeath() =>
            Instance.PlayerDeath();

        /// <summary>
        /// When a player leaves, remove them from all enmity tables.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void PlayerExit() =>
            Instance.PlayerExit();

        /// <summary>
        /// When a DM limbos creatures, ensure their enmity is wiped.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMLimboBefore)]
        public static void CreatureLimbo() =>
            Instance.CreatureLimbo();

        /// <summary>
        /// Retrieves a table containing the creatures on a specific enemy's enmity table.
        /// If no creatures are on the enmity table, an empty dictionary will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to use for retrieval</param>
        /// <returns>A dictionary containing an enemy's enmity table.</returns>
        public static Dictionary<uint, int> GetEnmityTable(uint enemy) =>
            Instance.GetEnmityTable(enemy);

        /// <summary>
        /// Retrieves the creature with the highest amount of enmity.
        /// If this cannot be determined, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to retrieve the highest target for.</param>
        /// <returns>The target with the highest enmity</returns>
        public static uint GetHighestEnmityTarget(uint enemy) =>
            Instance.GetHighestEnmityTarget(enemy);

        /// <summary>
        /// Retrieves the creature with the highest amount of enmity that the enemy can attack.
        /// Creatures hidden from the enemy by invisibility or undetected stealth keep their enmity but are skipped until
        /// they can be seen again. If no creature can be attacked, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to retrieve the attack target for.</param>
        /// <returns>The visible target with the highest enmity</returns>
        public static uint GetHighestEnmityAttackTarget(uint enemy) =>
            Instance.GetHighestEnmityAttackTarget(enemy);

        /// <summary>
        /// Makes every enemy with this creature on its enmity table choose its attack target again.
        /// Call after the creature becomes hidden so enemies react without waiting for their next
        /// heartbeat or combat round.
        /// </summary>
        /// <param name="creature">The creature whose enemies should re-evaluate their target.</param>
        public static void ReevaluateEnemyAttackTargets(uint creature) =>
            Instance.ReevaluateEnemyAttackTargets(creature);

        /// <summary>
        /// Determines whether a creature holds the enemy's attention by a safe margin: it must be the
        /// enemy's highest enmity target and lead the next-highest creature by at least
        /// <see cref="SecureThreatLeadPercent"/>. Taunts are wasted when this is already true.
        /// </summary>
        /// <param name="enemy">The enemy whose enmity table is checked.</param>
        /// <param name="creature">The creature expected to hold the enemy's attention.</param>
        /// <returns>true if the creature's threat lead is secure, false otherwise.</returns>
        public static bool IsThreatSecured(uint enemy, uint creature) =>
            Instance.IsThreatSecured(enemy, creature);

        public static bool IsThreatLeadSecure(int holderEnmity, int nextHighestEnmity) =>
            Instance.IsThreatLeadSecure(holderEnmity, nextHighestEnmity);

        /// <summary>
        /// Modifies the enmity of a specific target toward the specific creature.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The amount of enmity to adjust by</param>
        public static void ModifyEnmity(uint creature, uint enemy, int amount) =>
            Instance.ModifyEnmity(creature, enemy, amount);

        public static int CalculateAdjustedEnmity(int amount, int adjustment) =>
            Instance.CalculateAdjustedEnmity(amount, adjustment);

        /// <summary>
        /// Catches up to an eligible rival plus a fixed, stat-adjusted lead. Repeated taunts
        /// while already ahead do not accumulate threat; there is no target lock.
        /// </summary>
        public static bool TryTaunt(uint creature, uint enemy, int bonus) =>
            Instance.TryTaunt(creature, enemy, bonus);

        public static int CalculateTauntEnmity(int current, int rival, int adjustedBonus) =>
            Instance.CalculateTauntEnmity(current, rival, adjustedBonus);

        public static int ClampEnmityPercentAdjustment(int adjustment) =>
            Instance.ClampEnmityPercentAdjustment(adjustment);

        /// <summary>
        /// Modifies the enmity of all creatures who have the specified creature on their enmity table.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="amount">The amount of enmity to adjust by.</param>
        public static void ModifyEnmityOnAll(uint creature, int amount) =>
            Instance.ModifyEnmityOnAll(creature, amount);

        /// <summary>
        /// Reduces the creature's current enmity on every enemy table by a percentage.
        /// </summary>
        /// <param name="creature">The creature whose current enmity will be reduced.</param>
        /// <param name="percent">The percent of current enmity to remove.</param>
        public static void ReduceEnmityOnAll(uint creature, int percent) =>
            Instance.ReduceEnmityOnAll(creature, percent);

        /// <summary>
        /// Reduces the creature's current enmity on a single enemy table by a percentage.
        /// </summary>
        /// <param name="creature">The creature whose current enmity will be reduced.</param>
        /// <param name="enemy">The enemy whose enmity table will be adjusted.</param>
        /// <param name="percent">The percent of current enmity to remove.</param>
        public static void ReduceEnmity(uint creature, uint enemy, int percent) =>
            Instance.ReduceEnmity(creature, enemy, percent);

        public static int CalculateReducedEnmity(int current, int percent) =>
            Instance.CalculateReducedEnmity(current, percent);

        /// <summary>
        /// Modifies enmity from aggro proximity and tracks the resulting contribution separately.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The proximity enmity amount to apply.</param>
        public static void ModifyProximityEnmity(uint creature, uint enemy, int amount) =>
            Instance.ModifyProximityEnmity(creature, enemy, amount);

        /// <summary>
        /// Removes a creature from all enmity tables.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        public static void RemoveCreatureEnmity(uint creature) =>
            Instance.RemoveCreatureEnmity(creature);

        /// <summary>
        /// Removes a creature from one enemy's enmity table without making the enemy pick a new target.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        /// <param name="enemy">The enemy whose enmity table is updated.</param>
        public static void RemoveEnmity(uint creature, uint enemy) =>
            Instance.RemoveEnmity(creature, enemy);

        /// <summary>
        /// Removes the tracked proximity enmity contribution from a specific enemy's table.
        /// </summary>
        /// <param name="creature">The creature to remove proximity enmity for.</param>
        /// <param name="enemy">The enemy whose enmity table should be updated.</param>
        /// <returns>true if proximity enmity was removed.</returns>
        public static bool RemoveProximityEnmity(uint creature, uint enemy) =>
            Instance.RemoveProximityEnmity(creature, enemy);

        public static bool HasProximityEnmity(uint creature, uint enemy) =>
            Instance.HasProximityEnmity(creature, enemy);

        /// <summary>
        /// Returns true when a specific creature/enemy pair has enmity beyond the amount created
        /// solely by the enemy's aggro aura.
        /// </summary>
        public static bool HasNonProximityEnmity(uint creature, uint enemy) =>
            Instance.HasNonProximityEnmity(creature, enemy);

        /// <summary>
        /// Returns true when an enemy has any enmity that did not come solely from its aggro aura.
        /// Attack, damage, and ability enmity make the enemy an active combatant and therefore an
        /// invalid source for a new Espionage infiltration attempt.
        /// </summary>
        public static bool HasNonProximityEnmity(uint enemy) =>
            Instance.HasNonProximityEnmity(enemy);

        /// <summary>
        /// Returns true when a creature appears on any enemy table for more than aggro proximity.
        /// This distinguishes real combat from the proximity-only entries created as multiple
        /// stealthed players cross overlapping aggro auras.
        /// </summary>
        public static bool HasNonProximityEnmityForCreature(uint creature) =>
            Instance.HasNonProximityEnmityForCreature(creature);

        /// <summary>
        /// Returns true when either member of a creature/enemy pair has combat enmity involving
        /// someone outside that pair. Pair-specific checks use this to distinguish an expected
        /// aggro transition from unrelated combat.
        /// </summary>
        public static bool HasNonProximityEnmityOutsidePair(uint first, uint second) =>
            Instance.HasNonProximityEnmityOutsidePair(first, second);

        /// <summary>
        /// Clears every creature from an enemy's enmity table.
        /// </summary>
        /// <param name="enemy">The enemy whose enmity table will be cleared.</param>
        public static void ClearEnmityTable(uint enemy) =>
            Instance.ClearEnmityTable(enemy);

        /// <summary>
        /// Determines if a creature has enmity towards any other creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>true if creature has enmity on any other creature, false otherwise</returns>
        public static bool HasEnmity(uint creature) =>
            Instance.HasEnmity(creature);

        /// <summary>
        /// Resumes existing combat after the caller clears the action queue. The previous attack
        /// command is no longer pending, so its throttle must not delay the replacement command.
        /// </summary>
        public static void ResumeAttackAfterActionsCleared(uint creature) =>
            Instance.ResumeAttackAfterActionsCleared(creature);

        /// <summary>
        /// Forces a creature to attack the highest enmity target it can see.
        /// Stops an existing chase when its last proximity target is no longer in range.
        /// Stops attacking, but keeps its enmity, while every target is hidden by invisibility or stealth.
        /// If creature did not have enmity, nothing will happen.
        /// If the creature is already actively attacking that target, nothing will happen.
        /// </summary>
        public static void AttackHighestEnmityTarget(uint creature) =>
            Instance.AttackHighestEnmityTarget(creature);

        public static void IssueAttackCommand(uint creature, uint target, bool clearActions = true) =>
            Instance.IssueAttackCommand(creature, target, clearActions);

        /// <summary>
        /// Retrieves all of the enmity table information for a given creature.
        /// </summary>
        /// <param name="creature">The creature whose tables will be retrieved</param>
        /// <returns>A dictionary of enmity values for a given creature.</returns>
        public static Dictionary<uint, int> GetEnmityTowardsAllEnemies(uint creature) =>
            Instance.GetEnmityTowardsAllEnemies(creature);
    }
}
