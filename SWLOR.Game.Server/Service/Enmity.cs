using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class Enmity
    {
        public const int MinimumEnmityPercentAdjustment = -50;
        public const int MaximumEnmityPercentAdjustment = 50;
        // Enemy -> Creature -> EnmityAmount mapping
        private static readonly Dictionary<uint, Dictionary<uint, int>> _enemyEnmityTables = new();

        // Creature -> EnemyList mapping
        private static readonly Dictionary<uint, List<uint>> _creatureToEnemies = new();

        // Enemy -> Creature -> proximity enmity contribution mapping
        private static readonly Dictionary<uint, Dictionary<uint, int>> _proximityEnmityAmounts = new();
        private static readonly Dictionary<uint, DateTime> _attackCommandTimes = new();
        private const float MinimumStaleAttackRecoverySeconds = 4.5f;
        private const float AttackMoveRangeTolerance = 0.25f;
        private const float MeleeAttackMoveThreshold = 2.25f;

        /// <summary>
        /// When an enemy is damaged, increase enmity toward that creature by the amount of damage dealt.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDamagedBefore)]
        public static void CreatureDamaged()
        {
            var enemy = OBJECT_SELF;
            var damager = GetLastDamager(enemy);
            var damage = GetTotalDamageDealt();

            ModifyEnmity(damager, enemy, damage);
        }

        /// <summary>
        /// When a creature attacks an enemy, increase enmity by 1.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAttackBefore)]
        public static void CreatureAttacked()
        {
            var enemy = OBJECT_SELF;
            var attacker = GetLastAttacker(enemy);

            ModifyEnmity(attacker, enemy, 1);
        }

        /// <summary>
        /// When a creature dies, remove all enmity tables it is associated with.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void CreatureDeath()
        {
            var enemy = OBJECT_SELF;
            ClearEnmityTables(enemy);
            RemoveCreatureEnmity(enemy);
        }

        /// <summary>
        /// When a creature is destroyed with DestroyObject, remove all enmity tables it is associated with.
        /// </summary>
        [NWNEventHandler(ScriptName.OnObjectDestroyed)]
        public static void CreatureDestroyed()
        {
            var enemy = OBJECT_SELF;
            ClearEnmityTables(enemy);
            RemoveCreatureEnmity(enemy);
        }

        /// <summary>
        /// When a player dies, remove them from all enmity tables.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void PlayerDeath()
        {
            var player = GetLastPlayerDied();
            RemoveCreatureEnmity(player);
        }

        /// <summary>
        /// When a player leaves, remove them from all enmity tables.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void PlayerExit()
        {
            var player = GetExitingObject();
            RemoveCreatureEnmity(player);
        }

        /// <summary>
        /// When a DM limbos creatures, ensure their enmity is wiped.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMLimboBefore)]
        public static void CreatureLimbo()
        {
            var count = Convert.ToInt32(EventsPlugin.GetEventData("NUM_TARGETS"));

            for (var x = 1; x <= count; x++)
            {
                var targetData = EventsPlugin.GetEventData($"TARGET_{x}");

                if (uint.TryParse(targetData, out var target))
                {
                    ClearEnmityTables(target);
                    RemoveCreatureEnmity(target);
                }
            }
        }

        /// <summary>
        /// Retrieves a table containing the creatures on a specific enemy's enmity table.
        /// If no creatures are on the enmity table, an empty dictionary will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to use for retrieval</param>
        /// <returns>A dictionary containing an enemy's enmity table.</returns>
        public static Dictionary<uint, int> GetEnmityTable(uint enemy)
        {
            if(!_enemyEnmityTables.ContainsKey(enemy))
                return new Dictionary<uint, int>();

            return _enemyEnmityTables[enemy].ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves the creature with the highest amount of enmity.
        /// If this cannot be determined, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to retrieve the highest target for.</param>
        /// <returns>The target with the highest enmity</returns>
        public static uint GetHighestEnmityTarget(uint enemy)
        {
            var enmityTable = GetEnmityTable(enemy);
            var target = enmityTable.Count <= 0
                ? OBJECT_INVALID
                : enmityTable.MaxBy(o => o.Value).Key;

            return target;
        }

        /// <summary>
        /// Modifies the enmity of a specific target toward the specific creature.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The amount of enmity to adjust by</param>
        public static void ModifyEnmity(uint creature, uint enemy, int amount)
        {
            if (GetIsPC(enemy))
                return;

            if (AI.IsLeashEvading(enemy))
                return;

            // Enmity shouldn't matter if you're dead.
            if (GetIsDead(creature) || GetIsDead(enemy))
                return;

            // Players cannot be placed on an enmity table against each other.
            if (GetIsPC(creature) && GetIsPC(enemy))
                return;

            // Party members (droids, pets, associates) cannot gain enmity
            if (Party.IsInParty(creature, enemy))
                return;

            // Player associates cannot gain enmity towards each other
            if (GetIsPC(GetMaster(creature)) && GetIsPC(GetMaster(enemy)))
                return;

            // Value is zero, no action necessary.
            if (amount == 0) return;

            if (AI.TryStartCombatLeashEvade(enemy, creature))
                return;

            // Retrieve the creature's list of associated enemies.
            var enemyList = _creatureToEnemies.ContainsKey(creature) ? _creatureToEnemies[creature] : new List<uint>();

            // Fire off an event if this creature isn't currently on
            // any enmity lists already.
            if (enemyList.Count <= 0)
                ExecuteScript("enmity_acquired", creature);

            // Enemy isn't on the creature's list. Add it now.
            if (!enemyList.Contains(enemy))
                enemyList.Add(enemy);

            // Enemy doesn't have any tables yet.
            if (!_enemyEnmityTables.ContainsKey(enemy))
                _enemyEnmityTables[enemy] = new Dictionary<uint, int>();

            // This creature doesn't exist on the enemy's table yet.
            if (!_enemyEnmityTables[enemy].ContainsKey(creature))
                _enemyEnmityTables[enemy][creature] = 0;

            // Percent adjustment from feats/effects.
            var percentAdjustment = CalculateEnmityAdjustment(creature, enemy);
            amount += (int)(amount * (percentAdjustment * 0.01f));

            // Modify the enemy's enmity toward this creature.
            var enmityValue = _enemyEnmityTables[enemy][creature] + amount;

            // Enmity cannot fall below 1.
            if (enmityValue < 1)
                enmityValue = 1;

            // Update the enemy's enmity toward this creature.
            _enemyEnmityTables[enemy][creature] = enmityValue;

            // Update this creature's list of enemies.
            _creatureToEnemies[creature] = enemyList;

            AttackHighestEnmityTarget(enemy);

            ExecuteScript("enmity_changed", creature);
        }

        /// <summary>
        /// Determines the percent change that should be applied to enmity acquisition.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>The enmity adjustment percentage.</returns>
        private static int CalculateEnmityAdjustment(uint creature, uint enemy)
        {
            var adjustment = Stat.GetStatAdjustment(creature, StatType.EnmityPercentAdjustment) +
                             GetStatusSourceEnmityAdjustment(enemy, creature);
            return ClampEnmityPercentAdjustment(adjustment);
        }

        public static int ClampEnmityPercentAdjustment(int adjustment)
        {
            return Math.Clamp(
                adjustment,
                MinimumEnmityPercentAdjustment,
                MaximumEnmityPercentAdjustment);
        }

        /// <summary>
        /// Retrieves enmity adjustments from statuses on an enemy that point back to this creature.
        /// </summary>
        /// <param name="enemy">The enemy whose statuses are checked.</param>
        /// <param name="source">The creature the enemy is generating enmity toward.</param>
        /// <returns>The enmity adjustment percentage.</returns>
        private static int GetStatusSourceEnmityAdjustment(uint enemy, uint source)
        {
            if (!GetIsObjectValid(enemy) || !GetIsObjectValid(source))
                return 0;

            return StatusEffect.GetCreatureStatusEffects(enemy)
                .GetAllEffects()
                .Where(effect => effect.Source == source)
                .Select(effect =>
                {
                    effect.StatGroup.Stats.TryGetValue(StatType.EnmityToStatusSourcePercentAdjustment, out var adjustment);
                    return adjustment;
                })
                .DefaultIfEmpty(0)
                .Max();
        }

        /// <summary>
        /// Modifies the enmity of all creatures who have the specified creature on their enmity table.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="amount">The amount of enmity to adjust by.</param>
        public static void ModifyEnmityOnAll(uint creature, int amount)
        {
            // Value is zero, no action necessary.
            if (amount == 0) return;

            // Creature has no enemies.
            if (!_creatureToEnemies.ContainsKey(creature)) return;

            foreach (var enemy in _creatureToEnemies[creature])
            {
                ModifyEnmity(creature, enemy, amount);
            }
        }

        /// <summary>
        /// Reduces the creature's current enmity on every enemy table by a percentage.
        /// </summary>
        /// <param name="creature">The creature whose current enmity will be reduced.</param>
        /// <param name="percent">The percent of current enmity to remove.</param>
        public static void ReduceEnmityOnAll(uint creature, int percent)
        {
            if (percent <= 0)
                return;

            if (!_creatureToEnemies.ContainsKey(creature))
                return;

            foreach (var enemy in _creatureToEnemies[creature].ToArray())
            {
                ReduceEnmity(creature, enemy, percent);
            }
        }

        /// <summary>
        /// Reduces the creature's current enmity on a single enemy table by a percentage.
        /// </summary>
        /// <param name="creature">The creature whose current enmity will be reduced.</param>
        /// <param name="enemy">The enemy whose enmity table will be adjusted.</param>
        /// <param name="percent">The percent of current enmity to remove.</param>
        public static void ReduceEnmity(uint creature, uint enemy, int percent)
        {
            if (!_enemyEnmityTables.TryGetValue(enemy, out var table) ||
                !table.TryGetValue(creature, out var currentEnmity))
            {
                return;
            }

            var clampedPercent = Math.Min(percent, 100);
            var reduction = GameMath.PercentOf(currentEnmity, clampedPercent);
            table[creature] = Math.Max(1, currentEnmity - reduction);

            AttackHighestEnmityTarget(enemy);
            ExecuteScript("enmity_changed", creature);
        }

        /// <summary>
        /// Modifies enmity from aggro proximity and tracks the resulting contribution separately.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The proximity enmity amount to apply.</param>
        public static void ModifyProximityEnmity(uint creature, uint enemy, int amount)
        {
            if (amount == 0)
                return;

            var previousAmount = GetRawEnmityAmount(creature, enemy);
            ModifyEnmity(creature, enemy, amount);
            var currentAmount = GetRawEnmityAmount(creature, enemy);
            var appliedAmount = currentAmount - previousAmount;
            if (appliedAmount <= 0)
                return;

            if (!_proximityEnmityAmounts.TryGetValue(enemy, out var table))
            {
                table = new Dictionary<uint, int>();
                _proximityEnmityAmounts[enemy] = table;
            }

            table.TryGetValue(creature, out var existingAmount);
            table[creature] = existingAmount + appliedAmount;
        }

        /// <summary>
        /// Removes a creature from all enmity tables.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        public static void RemoveCreatureEnmity(uint creature)
        {
            _attackCommandTimes.Remove(creature);

            // Creature isn't on any enmity table.
            if (!_creatureToEnemies.ContainsKey(creature)) return;

            // Retrieve all of the creatures who have this creature on their enmity table.
            var enemies = _creatureToEnemies[creature].ToArray();
            foreach (var enemy in enemies)
            {
                RemoveEnmityTableEntry(creature, enemy);
                RemoveProximityEnmityTracking(creature, enemy);
            }
        }

        /// <summary>
        /// Removes the tracked proximity enmity contribution from a specific enemy's table.
        /// </summary>
        /// <param name="creature">The creature to remove proximity enmity for.</param>
        /// <param name="enemy">The enemy whose enmity table should be updated.</param>
        /// <returns>true if proximity enmity was removed.</returns>
        public static bool RemoveProximityEnmity(uint creature, uint enemy)
        {
            if (!_proximityEnmityAmounts.TryGetValue(enemy, out var proximityTable) ||
                !proximityTable.TryGetValue(creature, out var proximityAmount))
            {
                return false;
            }

            if (!_enemyEnmityTables.TryGetValue(enemy, out var table) ||
                !table.TryGetValue(creature, out var amount))
            {
                RemoveProximityEnmityTracking(creature, enemy);
                return false;
            }

            if (amount <= proximityAmount)
            {
                RemoveEnmityTableEntry(creature, enemy);
            }
            else
            {
                table[creature] = amount - proximityAmount;
            }

            RemoveProximityEnmityTracking(creature, enemy);
            return true;
        }

        public static bool HasProximityEnmity(uint creature, uint enemy)
        {
            return _proximityEnmityAmounts.TryGetValue(enemy, out var table) &&
                   table.ContainsKey(creature);
        }

        /// <summary>
        /// Returns true when a specific creature/enemy pair has enmity beyond the amount created
        /// solely by the enemy's aggro aura.
        /// </summary>
        public static bool HasNonProximityEnmity(uint creature, uint enemy)
        {
            return GetRawEnmityAmount(creature, enemy) > 0 &&
                   !HasOnlyProximityEnmity(creature, enemy);
        }

        /// <summary>
        /// Returns true when an enemy has any enmity that did not come solely from its aggro aura.
        /// Attack, damage, and ability enmity make the enemy an active combatant and therefore an
        /// invalid source for a new Espionage infiltration attempt.
        /// </summary>
        public static bool HasNonProximityEnmity(uint enemy)
        {
            if (!_enemyEnmityTables.TryGetValue(enemy, out var table))
                return false;

            return table.Keys.Any(creature => HasNonProximityEnmity(creature, enemy));
        }

        /// <summary>
        /// Returns true when a creature appears on any enemy table for more than aggro proximity.
        /// This distinguishes real combat from the proximity-only entries created as multiple
        /// stealthed players cross overlapping aggro auras.
        /// </summary>
        public static bool HasNonProximityEnmityForCreature(uint creature)
        {
            if (!_creatureToEnemies.TryGetValue(creature, out var enemies))
                return false;

            return enemies.Any(enemy => HasNonProximityEnmity(creature, enemy));
        }

        /// <summary>
        /// Returns true when either member of a creature/enemy pair has combat enmity involving
        /// someone outside that pair. Pair-specific checks use this to distinguish an expected
        /// aggro transition from unrelated combat.
        /// </summary>
        public static bool HasNonProximityEnmityOutsidePair(uint first, uint second)
        {
            return HasNonProximityEnmityAsCreatureOutsidePair(first, second) ||
                   HasNonProximityEnmityAsEnemyOutsidePair(first, second) ||
                   HasNonProximityEnmityAsCreatureOutsidePair(second, first) ||
                   HasNonProximityEnmityAsEnemyOutsidePair(second, first);
        }

        private static bool HasNonProximityEnmityAsCreatureOutsidePair(uint creature, uint pairedEnemy)
        {
            return _creatureToEnemies.TryGetValue(creature, out var enemies) &&
                   enemies.Any(enemy =>
                       enemy != pairedEnemy && HasNonProximityEnmity(creature, enemy));
        }

        private static bool HasNonProximityEnmityAsEnemyOutsidePair(uint enemy, uint pairedCreature)
        {
            return _enemyEnmityTables.TryGetValue(enemy, out var table) &&
                   table.Keys.Any(creature =>
                       creature != pairedCreature && HasNonProximityEnmity(creature, enemy));
        }

        /// <summary>
        /// Clears every creature from an enemy's enmity table.
        /// </summary>
        /// <param name="enemy">The enemy whose enmity table will be cleared.</param>
        public static void ClearEnmityTable(uint enemy)
        {
            ClearEnmityTables(enemy);
        }

        /// <summary>
        /// Clears an enemy's enmity tables and removes associated creatures from cache.
        /// </summary>
        /// <param name="enemy">The enemy whose tables we're clearing</param>
        private static void ClearEnmityTables(uint enemy)
        {
            _attackCommandTimes.Remove(enemy);

            // Enemy isn't registered as having an enmity table.
            if (!_enemyEnmityTables.ContainsKey(enemy))
            {
                _proximityEnmityAmounts.Remove(enemy);
                return;
            }

            // For every creature on this enemy's enmity table,
            // remove the enemy from that creature's list.
            var creatures = _enemyEnmityTables[enemy];
            foreach (var (creature, _) in creatures)
            {
                if (!_creatureToEnemies.TryGetValue(creature, out var enemies))
                    continue;

                enemies.Remove(enemy);
                if (enemies.Count <= 0)
                {
                    _creatureToEnemies.Remove(creature);
                }
            }

            _enemyEnmityTables.Remove(enemy);
            _proximityEnmityAmounts.Remove(enemy);
        }

        /// <summary>
        /// Determines if a creature has enmity towards any other creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>true if creature has enmity on any other creature, false otherwise</returns>
        public static bool HasEnmity(uint creature)
        {
            return _creatureToEnemies.ContainsKey(creature)
                   && _creatureToEnemies[creature].Count > 0;
        }

        /// <summary>
        /// Forces a creature to attack the highest enmity target.
        /// If creature does not have enmity, nothing will happen.
        /// If the creature is already actively attacking that target, nothing will happen.
        /// </summary>
        public static void AttackHighestEnmityTarget(uint creature)
        {
            var target = GetHighestEnmityTarget(creature);
            while (GetIsObjectValid(target) && ShouldRemoveStaleProximityTarget(creature, target))
            {
                RemoveProximityEnmity(target, creature);
                target = GetHighestEnmityTarget(creature);
            }

            if (!GetIsObjectValid(target) ||
                GetArea(creature) != GetArea(target))
                return;

            if (AI.TryStartCombatLeashEvade(creature, target))
                return;

            // Same target - no need to switch.
            var attackTarget = GetAttackTarget(creature);
            var currentAction = GetCurrentAction(creature);
            var isBusy = Activity.IsBusy(creature);
            var shouldRecoverStaleAttack = ShouldRecoverStaleAttack(
                creature,
                attackTarget,
                target,
                currentAction);
            _attackCommandTimes.TryGetValue(creature, out var commandTime);
            var commandIssuedAt = commandTime == default
                ? (DateTime?)null
                : commandTime;
            var recoverySeconds = GetStaleAttackRecoverySeconds(creature);

            if (!ShouldIssueAttackCommand(
                    attackTarget,
                    target,
                    currentAction,
                    isBusy,
                    shouldRecoverStaleAttack,
                    DateTime.UtcNow,
                    commandIssuedAt,
                    recoverySeconds))
            {
                return;
            }

            if (shouldRecoverStaleAttack)
                Log.Write(LogGroup.AI, $"{GetName(creature)} recovered stale attack action against {GetName(target)}.");

            IssueAttackCommand(creature, target);
        }

        public static void IssueAttackCommand(uint creature, uint target, bool clearActions = true)
        {
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(target) ||
                GetArea(creature) != GetArea(target))
            {
                return;
            }

            if (AI.TryStartCombatLeashEvade(creature, target))
                return;

            _attackCommandTimes[creature] = DateTime.UtcNow;
            AssignCommand(creature, () =>
            {
                if (AI.TryStartCombatLeashEvade(creature, target))
                    return;

                if (clearActions)
                    ClearAllActions(true);

                if (ShouldMoveIntoAttackRange(creature, target))
                    ActionMoveToObject(target, true, GetAttackMoveRange(creature));

                ActionDoCommand(() =>
                {
                    if (AI.TryStartCombatLeashEvade(creature, target))
                        return;

                    ActionAttack(target);
                });
            });
        }

        private static bool ShouldIssueAttackCommand(
            uint attackTarget,
            uint desiredTarget,
            ActionType currentAction,
            bool isBusy,
            bool shouldRecoverStaleAttack,
            DateTime now,
            DateTime? commandIssuedAt,
            float recoverySeconds)
        {
            if (isBusy)
                return false;

            if (shouldRecoverStaleAttack)
                return true;

            if (attackTarget != OBJECT_INVALID && attackTarget != desiredTarget)
                return true;

            if (HasRecentAttackCommand(now, commandIssuedAt, recoverySeconds))
                return false;

            return currentAction != ActionType.AttackObject;
        }

        private static bool HasRecentAttackCommand(DateTime now, DateTime? commandIssuedAt, float recoverySeconds)
        {
            return commandIssuedAt != null &&
                   (now - commandIssuedAt.Value).TotalSeconds < recoverySeconds;
        }

        private static bool ShouldMoveIntoAttackRange(uint creature, uint target)
        {
            if (GetIsPC(creature) ||
                !GetIsObjectValid(target))
            {
                return false;
            }

            var skillType = Combat.GetEquippedWeaponSkillType(creature);
            var moveRange = Combat.GetWeaponEngagementRange(skillType);

            return ShouldMoveIntoAttackRange(GetDistanceBetween(creature, target), skillType, moveRange);
        }

        private static bool ShouldMoveIntoAttackRange(float distance, SkillType skillType, float moveRange)
        {
            var threshold = Combat.IsRangedWeaponSkill(skillType)
                ? moveRange + AttackMoveRangeTolerance
                : MeleeAttackMoveThreshold;

            return distance > threshold;
        }

        private static float GetAttackMoveRange(uint creature)
        {
            var skillType = Combat.GetEquippedWeaponSkillType(creature);
            return Combat.GetWeaponEngagementRange(skillType);
        }

        private static bool ShouldRemoveStaleProximityTarget(uint enemy, uint target)
        {
            return HasOnlyProximityEnmity(target, enemy) &&
                   !AI.IsInAggroRange(enemy, target);
        }

        private static bool HasOnlyProximityEnmity(uint creature, uint enemy)
        {
            var rawAmount = GetRawEnmityAmount(creature, enemy);
            if (rawAmount <= 0)
                return false;

            var proximityAmount = GetProximityEnmityAmount(creature, enemy);
            return proximityAmount >= rawAmount;
        }

        private static bool ShouldRecoverStaleAttack(
            uint creature,
            uint attackTarget,
            uint desiredTarget,
            ActionType currentAction)
        {
            _attackCommandTimes.TryGetValue(creature, out var commandTime);
            var commandIssuedAt = commandTime == default
                ? (DateTime?)null
                : commandTime;
            var recoverySeconds = GetStaleAttackRecoverySeconds(creature);

            return ShouldRecoverStaleAttack(
                attackTarget,
                desiredTarget,
                currentAction,
                DateTime.UtcNow,
                commandIssuedAt,
                Combat.HasRecentAttackActivity(creature, recoverySeconds),
                recoverySeconds);
        }

        private static bool ShouldRecoverStaleAttack(
            uint attackTarget,
            uint desiredTarget,
            ActionType currentAction,
            DateTime now,
            DateTime? commandIssuedAt,
            bool hasRecentAttack,
            float recoverySeconds)
        {
            if (attackTarget != desiredTarget ||
                currentAction != ActionType.AttackObject ||
                hasRecentAttack)
            {
                return false;
            }

            if (commandIssuedAt == null)
                return true;

            return (now - commandIssuedAt.Value).TotalSeconds >= recoverySeconds;
        }

        private static float GetStaleAttackRecoverySeconds(uint creature)
        {
            var calculatedDelay = Combat.CalculateAttackDelay(creature);
            var effectiveDelay = Combat.CalculateEffectiveAttackDelay(calculatedDelay);

            return GetStaleAttackRecoverySeconds(effectiveDelay);
        }

        private static float GetStaleAttackRecoverySeconds(int effectiveDelayMilliseconds)
        {
            // Attacks arrive in swings; fast delays resolve multiple attacks per swing,
            // so staleness is measured against the swing cadence rather than the per-attack delay.
            var swingDelaySeconds = Combat.CalculateAttackSwingDelay(effectiveDelayMilliseconds) / 1000f;

            return Math.Max(MinimumStaleAttackRecoverySeconds, swingDelaySeconds * 2f + 1f);
        }

        /// <summary>
        /// Retrieves all of the enmity table information for a given creature.
        /// </summary>
        /// <param name="creature">The creature whose tables will be retrieved</param>
        /// <returns>A dictionary of enmity values for a given creature.</returns>
        public static Dictionary<uint, int> GetEnmityTowardsAllEnemies(uint creature)
        {
            var enemyList = _creatureToEnemies.ContainsKey(creature)
                ? _creatureToEnemies[creature]
                : new List<uint>();

            var result = new Dictionary<uint, int>();

            foreach (var enemy in enemyList)
            {
                if(!_enemyEnmityTables.ContainsKey(enemy) ||
                   !_enemyEnmityTables[enemy].ContainsKey(creature))
                    continue;

                var enmity = _enemyEnmityTables[enemy][creature];

                result.Add(enemy, enmity);
            }

            return result;
        }

        private static int GetRawEnmityAmount(uint creature, uint enemy)
        {
            return _enemyEnmityTables.TryGetValue(enemy, out var table) &&
                   table.TryGetValue(creature, out var amount)
                ? amount
                : 0;
        }

        private static int GetProximityEnmityAmount(uint creature, uint enemy)
        {
            return _proximityEnmityAmounts.TryGetValue(enemy, out var table) &&
                   table.TryGetValue(creature, out var amount)
                ? amount
                : 0;
        }

        private static void RemoveEnmityTableEntry(uint creature, uint enemy)
        {
            if (_enemyEnmityTables.TryGetValue(enemy, out var table))
            {
                table.Remove(creature);
                if (table.Count <= 0)
                {
                    _enemyEnmityTables.Remove(enemy);
                }
            }

            if (!_creatureToEnemies.TryGetValue(creature, out var enemies))
                return;

            enemies.Remove(enemy);
            if (enemies.Count <= 0)
            {
                _creatureToEnemies.Remove(creature);
            }
        }

        private static void RemoveProximityEnmityTracking(uint creature, uint enemy)
        {
            if (!_proximityEnmityAmounts.TryGetValue(enemy, out var table))
                return;

            table.Remove(creature);
            if (table.Count <= 0)
            {
                _proximityEnmityAmounts.Remove(enemy);
            }
        }
    }
}
