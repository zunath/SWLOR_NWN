using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CompanionControlService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Who each enemy hates and why: the enmity tables, the aggro-proximity share of them, the attack commands that
    /// follow the highest target, and the taunt, healing and damage rules that feed them. The logic that used to be
    /// static in <c>Service.Enmity</c> is moved here unchanged; the tables are this instance's own fields and are read
    /// per damage event and per attack without being rebuilt. The event handlers run on the container's instance; static
    /// callers reach it through <c>GameServices.Enmity</c>. Calls into the unconverted hot core and feature code sit behind one-line
    /// <c>protected virtual</c> methods whose bodies are the original calls.
    /// </summary>
    public class EnmityService : IEnmityService
    {
        private readonly IAIService _ai;
        private readonly IActivityService _activity;
        private readonly IPartyService _party;
        private readonly IHoloComService _holoCom;
        private readonly IBeastMasteryService _beastMastery;
        private readonly Lazy<IStatService> _stat;
        private readonly IStealthService _stealth;
        private readonly ICompanionControlService _companionControl;
        private readonly IStatusEffectService _statusEffect;
        private readonly Lazy<ICombatActivityService> _combatActivity;
        private readonly Lazy<ICombatAttackTimingService> _attackTiming;
        private readonly Lazy<ICombatQueriesService> _queries;

        private readonly Lazy<IDroidService> _droid;

        public EnmityService(
            IAIService ai,
            IActivityService activity,
            IPartyService party,
            IHoloComService holoCom,
            IBeastMasteryService beastMastery,
            Lazy<IStatService> stat,
            IStealthService stealth,
            ICompanionControlService companionControl,
            IStatusEffectService statusEffect,
            Lazy<ICombatActivityService> combatActivity,
            Lazy<ICombatAttackTimingService> attackTiming,
            Lazy<ICombatQueriesService> queries,
            Lazy<IDroidService> droid)
        {
            _droid = droid;
            _ai = ai;
            _activity = activity;
            _party = party;
            _holoCom = holoCom;
            _beastMastery = beastMastery;
            _stat = stat;
            _stealth = stealth;
            _companionControl = companionControl;
            _statusEffect = statusEffect;
            _combatActivity = combatActivity;
            _attackTiming = attackTiming;
            _queries = queries;
        }

        public const int MinimumEnmityPercentAdjustment = -50;
        public const int MaximumEnmityPercentAdjustment = 50;
        public const int SecureThreatLeadPercent = 25;
        // Enemy -> Creature -> EnmityAmount mapping
        private readonly Dictionary<uint, Dictionary<uint, int>> _enemyEnmityTables = new();

        // Creature -> EnemyList mapping
        private readonly Dictionary<uint, List<uint>> _creatureToEnemies = new();

        // Enemy -> Creature -> proximity enmity contribution mapping
        private readonly Dictionary<uint, Dictionary<uint, int>> _proximityEnmityAmounts = new();
        private readonly Dictionary<uint, DateTime> _attackCommandTimes = new();
        private readonly Dictionary<uint, uint> _attackCommandTargets = new();
        private const float MinimumStaleAttackRecoverySeconds = 4.5f;
        private const float AttackMoveRangeTolerance = 0.25f;
        private const float MeleeAttackMoveThreshold = 2.25f;

        /// <summary>
        /// When an enemy is damaged, increase enmity toward that creature by the amount of damage dealt.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDamagedBefore)]
        public void CreatureDamaged()
        {
            var enemy = OBJECT_SELF;
            var damager = GetLastDamager(enemy);
            var damage = GetTotalDamageDealt();

            if (damage <= 0 || !GetIsObjectValid(damager))
                return;
            ModifyEnmity(damager, enemy, CalculateDamageEnmity(damage,
                GetStatAdjustment(damager, StatType.DamageEnmityPercentAdjustment)));
        }

        public int CalculateDamageEnmity(int damage, int adjustment)
        {
            return damage <= 0 ? 0 : (int)Math.Clamp((long)damage * Math.Max(0L, 100L + adjustment) / 100, 1, int.MaxValue);
        }

        // Healing events can nest when healing riders fire. Pair each before/after observation.
        private readonly Dictionary<uint, Stack<(uint Source, int HitPoints, long NestedHealing)>> _healingHitPoints = new();
        [NWNEventHandler(ScriptName.OnHealBefore)]
        public void HealingStarted()
        {
            var target = StringToObject(EventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            if (!GetIsObjectValid(target))
                return;
            if (!_healingHitPoints.TryGetValue(target, out var values))
                _healingHitPoints[target] = values = new Stack<(uint, int, long)>();
            values.Push((OBJECT_SELF, GetCurrentHitPoints(target), 0));
        }
        [NWNEventHandler(ScriptName.OnHealAfter)]
        public void HealingCompleted()
        {
            var source = OBJECT_SELF;
            var target = StringToObject(EventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            if (!_healingHitPoints.TryGetValue(target, out var values) || values.Count == 0)
                return;
            var observation = values.Pop();
            var previousHP = observation.HitPoints;
            var currentHP = GetCurrentHitPoints(target);
            if (values.Count > 0)
            {
                // Propagate the child's entire HP gain, including its descendants.
                // Preserve the original HP so nested healing cannot turn a resurrection into a heal.
                var parent = values.Pop();
                values.Push((parent.Source, parent.HitPoints,
                    parent.NestedHealing + Math.Max(0L, (long)currentHP - previousHP)));
            }
            if (values.Count == 0)
                _healingHitPoints.Remove(target);
            if (observation.Source != source)
                return;
            if (!int.TryParse(EventsPlugin.GetEventData("HEAL_AMOUNT"), out var requested))
                return;

            var amount = CalculateHealingEnmity(requested, previousHP, currentHP, observation.NestedHealing);
            if (amount <= 0 || !GetIsObjectValid(source))
                return;
            foreach (var enemy in GetEnmityTowardsAllEnemies(target).Keys)
            {
                if (HasNonProximityEnmity(target, enemy))
                    ModifyEnmity(source, enemy, amount);
            }
        }

        public int CalculateHealingEnmity(int requested, int previousHP, int currentHP, long nestedHealing = 0)
        {
            if (previousHP <= 0)
                return 0;
            var restored = Math.Min(Math.Max(0, requested),
                Math.Max(0L, (long)currentHP - previousHP - Math.Max(0, nestedHealing)));
            return (int)(restored / 2);
        }

        /// <summary>
        /// When a creature attacks an enemy, increase enmity by 1.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAttackBefore)]
        public void CreatureAttacked()
        {
            var enemy = OBJECT_SELF;
            var attacker = GetLastAttacker(enemy);

            ModifyEnmity(attacker, enemy, 1);
        }

        /// <summary>
        /// When a creature dies, remove all enmity tables it is associated with.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public void CreatureDeath()
        {
            var enemy = OBJECT_SELF;
            ClearEnmityTables(enemy);
            RemoveCreatureEnmity(enemy);
        }

        /// <summary>
        /// When a creature is destroyed with DestroyObject, remove all enmity tables it is associated with.
        /// </summary>
        [NWNEventHandler(ScriptName.OnObjectDestroyed)]
        public void CreatureDestroyed()
        {
            var enemy = OBJECT_SELF;
            ClearEnmityTables(enemy);
            RemoveCreatureEnmity(enemy);
        }

        /// <summary>
        /// When a player dies, remove them from all enmity tables.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public void PlayerDeath()
        {
            var player = GetLastPlayerDied();
            RemoveCreatureEnmity(player);
        }

        /// <summary>
        /// When a player leaves, remove them from all enmity tables.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public void PlayerExit()
        {
            var player = GetExitingObject();
            RemoveCreatureEnmity(player);
        }

        /// <summary>
        /// When a DM limbos creatures, ensure their enmity is wiped.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMLimboBefore)]
        public void CreatureLimbo()
        {
            var count = Convert.ToInt32(EventsPlugin.GetEventData("NUM_TARGETS"));

            for (var x = 1; x <= count; x++)
            {
                var target = StringToObject(EventsPlugin.GetEventData($"TARGET_{x}"));

                if (GetIsObjectValid(target))
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
        public Dictionary<uint, int> GetEnmityTable(uint enemy)
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
        public uint GetHighestEnmityTarget(uint enemy)
        {
            var enmityTable = GetEnmityTable(enemy);
            var target = enmityTable.Count <= 0
                ? OBJECT_INVALID
                : enmityTable.MaxBy(o => o.Value).Key;

            return target;
        }

        /// <summary>
        /// Retrieves the creature with the highest amount of enmity that the enemy can attack.
        /// Creatures hidden from the enemy by invisibility or undetected stealth keep their enmity but are skipped until
        /// they can be seen again. If no creature can be attacked, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="enemy">The enemy to retrieve the attack target for.</param>
        /// <returns>The visible target with the highest enmity</returns>
        public uint GetHighestEnmityAttackTarget(uint enemy)
        {
            // OrderByDescending is stable, so ties resolve to the same creature as GetHighestEnmityTarget.
            foreach (var (creature, _) in GetEnmityTable(enemy).OrderByDescending(entry => entry.Value))
            {
                if (IsEligibleAttackTarget(enemy, creature))
                    return creature;
            }

            return OBJECT_INVALID;
        }

        private bool IsEligibleAttackTarget(uint enemy, uint creature)
        {
            return GetIsObjectValid(creature) && !GetIsDead(creature) &&
                   GetCurrentHitPoints(creature) > 0 && GetArea(enemy) == GetArea(creature) &&
                   !IsHiddenFrom(enemy, creature);
        }

        /// <summary>
        /// Makes every enemy with this creature on its enmity table choose its attack target again.
        /// Call after the creature becomes hidden so enemies react without waiting for their next
        /// heartbeat or combat round.
        /// </summary>
        /// <param name="creature">The creature whose enemies should re-evaluate their target.</param>
        public void ReevaluateEnemyAttackTargets(uint creature)
        {
            if (!_creatureToEnemies.TryGetValue(creature, out var enemies))
                return;

            foreach (var enemy in enemies.ToArray())
            {
                AttackHighestEnmityTarget(enemy);
            }
        }

        /// <summary>
        /// Determines whether a creature holds the enemy's attention by a safe margin: it must be the
        /// enemy's highest enmity target and lead the next-highest creature by at least
        /// <see cref="SecureThreatLeadPercent"/>. Taunts are wasted when this is already true.
        /// </summary>
        /// <param name="enemy">The enemy whose enmity table is checked.</param>
        /// <param name="creature">The creature expected to hold the enemy's attention.</param>
        /// <returns>true if the creature's threat lead is secure, false otherwise.</returns>
        public bool IsThreatSecured(uint enemy, uint creature)
        {
            if (!_enemyEnmityTables.TryGetValue(enemy, out var table) ||
                !table.TryGetValue(creature, out var creatureEnmity))
            {
                return false;
            }

            var nextHighest = 0;
            foreach (var (other, amount) in table)
            {
                if (other != creature && amount > nextHighest)
                    nextHighest = amount;
            }

            return IsThreatLeadSecure(creatureEnmity, nextHighest);
        }

        public bool IsThreatLeadSecure(int holderEnmity, int nextHighestEnmity)
        {
            if (holderEnmity <= 0 || holderEnmity < nextHighestEnmity)
                return false;

            return (long)holderEnmity * 100 >= (long)nextHighestEnmity * (100 + SecureThreatLeadPercent);
        }

        /// <summary>
        /// Modifies the enmity of a specific target toward the specific creature.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The amount of enmity to adjust by</param>
        public void ModifyEnmity(uint creature, uint enemy, int amount)
        {
            ModifyEnmity(creature, enemy, amount, true);
        }

        private void ModifyEnmity(uint creature, uint enemy, int amount, bool adjustGeneration)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(enemy) || creature == enemy ||
                GetArea(creature) != GetArea(enemy))
                return;

            if (GetIsPC(enemy))
                return;

            if (_holoCom.IsHologram(creature) || _holoCom.IsHologram(enemy))
                return;

            if (_ai.IsLeashEvading(enemy))
                return;

            // Enmity shouldn't matter if you're dead.
            if (GetIsDead(creature) || GetIsDead(enemy))
                return;

            // Players cannot be placed on an enmity table against each other.
            if (GetIsPC(creature) && GetIsPC(enemy))
                return;

            // Party members (droids, pets, associates) cannot gain enmity
            if (_party.IsInParty(creature, enemy))
                return;

            // Player associates cannot gain enmity towards each other
            if (GetIsPC(GetMaster(creature)) && GetIsPC(GetMaster(enemy)))
                return;

            // Value is zero, no action necessary.
            if (amount == 0) return;

            if (_ai.TryStartCombatLeashEvade(enemy, creature))
                return;

            // Retrieve the creature's list of associated enemies.
            var enemyList = _creatureToEnemies.ContainsKey(creature) ? _creatureToEnemies[creature] : new List<uint>();

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
            if (adjustGeneration)
                amount = CalculateAdjustedEnmity(amount, CalculateEnmityAdjustment(creature, enemy));

            // Modify the enemy's enmity toward this creature.
            var enmityValue = (int)Math.Clamp((long)_enemyEnmityTables[enemy][creature] + amount, 1, int.MaxValue);

            // Enmity cannot fall below 1.
            if (enmityValue < 1)
                enmityValue = 1;

            // Update the enemy's enmity toward this creature.
            _enemyEnmityTables[enemy][creature] = enmityValue;

            // Update this creature's list of enemies.
            _creatureToEnemies[creature] = enemyList;

            AttackHighestEnmityTarget(enemy);

            ExecuteScript(ScriptName.OnEnmityChanged, creature);
        }

        public int CalculateAdjustedEnmity(int amount, int adjustment)
        {
            // Generation modifiers must not make threat reductions stronger or weaker.
            return amount <= 0 ? amount : (int)Math.Clamp(
                (long)amount * (100 + ClampEnmityPercentAdjustment(adjustment)) / 100, 1, int.MaxValue);
        }

        /// <summary>
        /// Catches up to an eligible rival plus a fixed, stat-adjusted lead. Repeated taunts
        /// while already ahead do not accumulate threat; there is no target lock.
        /// </summary>
        public bool TryTaunt(uint creature, uint enemy, int bonus)
        {
            if (bonus <= 0 || !GetIsObjectValid(creature) || !GetIsObjectValid(enemy) ||
                GetIsDead(creature) || GetIsDead(enemy) || GetIsPC(enemy) ||
                GetIsObjectValid(GetMaster(enemy)) || _beastMastery.IsPlayerBeast(enemy) || IsDroid(enemy) ||
                GetArea(creature) != GetArea(enemy) || !GetIsReactionTypeHostile(enemy, creature) ||
                !LineOfSightObject(creature, enemy) || _ai.IsLeashEvading(enemy) ||
                _ai.TryStartCombatLeashEvade(enemy, creature))
                return false;

            var rival = GetEnmityTable(enemy)
                .Where(entry => entry.Key != creature && IsEligibleAttackTarget(enemy, entry.Key))
                .Select(entry => entry.Value).DefaultIfEmpty(0).Max();
            var desired = CalculateTauntEnmity(GetRawEnmityAmount(creature, enemy), rival,
                CalculateAdjustedEnmity(bonus, CalculateEnmityAdjustment(creature, enemy)));
            if (desired > GetRawEnmityAmount(creature, enemy))
            {
                // Register through the ordinary path without scaling the catch-up twice.
                ModifyEnmity(creature, enemy, desired - GetRawEnmityAmount(creature, enemy), false);
                if (!_enemyEnmityTables.TryGetValue(enemy, out var table) || !table.ContainsKey(creature))
                    return false;
            }
            else
                AttackHighestEnmityTarget(enemy);
            return true;
        }

        public int CalculateTauntEnmity(int current, int rival, int adjustedBonus)
        {
            return (int)Math.Clamp(Math.Max((long)current, (long)rival + Math.Max(1, adjustedBonus)), 1, int.MaxValue);
        }

        /// <summary>
        /// Determines the percent change that should be applied to enmity acquisition.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>The enmity adjustment percentage.</returns>
        private int CalculateEnmityAdjustment(uint creature, uint enemy)
        {
            var adjustment = GetStatAdjustment(creature, StatType.EnmityPercentAdjustment) +
                             GetStatusSourceEnmityAdjustment(enemy, creature);
            return ClampEnmityPercentAdjustment(adjustment);
        }

        public int ClampEnmityPercentAdjustment(int adjustment)
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
        private int GetStatusSourceEnmityAdjustment(uint enemy, uint source)
        {
            if (!GetIsObjectValid(enemy) || !GetIsObjectValid(source))
                return 0;

            return GetAllStatusEffects(enemy)
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
        public void ModifyEnmityOnAll(uint creature, int amount)
        {
            // Value is zero, no action necessary.
            if (amount == 0) return;

            // Creature has no enemies.
            if (!_creatureToEnemies.ContainsKey(creature)) return;

            foreach (var enemy in _creatureToEnemies[creature].ToArray())
            {
                ModifyEnmity(creature, enemy, amount);
            }
        }

        /// <summary>
        /// Reduces the creature's current enmity on every enemy table by a percentage.
        /// </summary>
        /// <param name="creature">The creature whose current enmity will be reduced.</param>
        /// <param name="percent">The percent of current enmity to remove.</param>
        public void ReduceEnmityOnAll(uint creature, int percent)
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
        public void ReduceEnmity(uint creature, uint enemy, int percent)
        {
            if (percent <= 0)
                return;
            if (!_enemyEnmityTables.TryGetValue(enemy, out var table) ||
                !table.TryGetValue(creature, out var currentEnmity))
            {
                return;
            }

            table[creature] = CalculateReducedEnmity(currentEnmity, percent);
            if (_proximityEnmityAmounts.TryGetValue(enemy, out var proximity) && proximity.TryGetValue(creature, out var amount))
                proximity[creature] = Math.Min(table[creature], CalculateReducedEnmity(amount, percent));

            AttackHighestEnmityTarget(enemy);
            ExecuteScript(ScriptName.OnEnmityChanged, creature);
        }

        public int CalculateReducedEnmity(int current, int percent)
        {
            var reduction = ((long)Math.Max(0, current) * Math.Clamp(percent, 0, 100) + 99) / 100;
            return (int)Math.Max(1, current - reduction);
        }

        /// <summary>
        /// Modifies enmity from aggro proximity and tracks the resulting contribution separately.
        /// </summary>
        /// <param name="creature">The creature whose enmity will be increased.</param>
        /// <param name="enemy">The enemy who will have raised enmity toward creature.</param>
        /// <param name="amount">The proximity enmity amount to apply.</param>
        public void ModifyProximityEnmity(uint creature, uint enemy, int amount)
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
        public void RemoveCreatureEnmity(uint creature)
        {
            _attackCommandTimes.Remove(creature);
            _attackCommandTargets.Remove(creature);

            // Creature isn't on any enmity table.
            if (!_creatureToEnemies.ContainsKey(creature)) return;

            // Retrieve all of the creatures who have this creature on their enmity table.
            var enemies = _creatureToEnemies[creature].ToArray();
            foreach (var enemy in enemies)
            {
                RemoveEnmityTableEntry(creature, enemy);
                RemoveProximityEnmityTracking(creature, enemy);
                AttackHighestEnmityTarget(enemy);
            }
        }

        /// <summary>
        /// Removes a creature from one enemy's enmity table without making the enemy pick a new target.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        /// <param name="enemy">The enemy whose enmity table is updated.</param>
        public void RemoveEnmity(uint creature, uint enemy)
        {
            RemoveEnmityTableEntry(creature, enemy);
            RemoveProximityEnmityTracking(creature, enemy);
        }

        /// <summary>
        /// Removes the tracked proximity enmity contribution from a specific enemy's table.
        /// </summary>
        /// <param name="creature">The creature to remove proximity enmity for.</param>
        /// <param name="enemy">The enemy whose enmity table should be updated.</param>
        /// <returns>true if proximity enmity was removed.</returns>
        public bool RemoveProximityEnmity(uint creature, uint enemy)
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

        public bool HasProximityEnmity(uint creature, uint enemy)
        {
            return _proximityEnmityAmounts.TryGetValue(enemy, out var table) &&
                   table.ContainsKey(creature);
        }

        /// <summary>
        /// Returns true when a specific creature/enemy pair has enmity beyond the amount created
        /// solely by the enemy's aggro aura.
        /// </summary>
        public bool HasNonProximityEnmity(uint creature, uint enemy)
        {
            return GetRawEnmityAmount(creature, enemy) > 0 &&
                   !HasOnlyProximityEnmity(creature, enemy);
        }

        /// <summary>
        /// Returns true when an enemy has any enmity that did not come solely from its aggro aura.
        /// Attack, damage, and ability enmity make the enemy an active combatant and therefore an
        /// invalid source for a new Espionage infiltration attempt.
        /// </summary>
        public bool HasNonProximityEnmity(uint enemy)
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
        public bool HasNonProximityEnmityForCreature(uint creature)
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
        public bool HasNonProximityEnmityOutsidePair(uint first, uint second)
        {
            return HasNonProximityEnmityAsCreatureOutsidePair(first, second) ||
                   HasNonProximityEnmityAsEnemyOutsidePair(first, second) ||
                   HasNonProximityEnmityAsCreatureOutsidePair(second, first) ||
                   HasNonProximityEnmityAsEnemyOutsidePair(second, first);
        }

        private bool HasNonProximityEnmityAsCreatureOutsidePair(uint creature, uint pairedEnemy)
        {
            return _creatureToEnemies.TryGetValue(creature, out var enemies) &&
                   enemies.Any(enemy =>
                       enemy != pairedEnemy && HasNonProximityEnmity(creature, enemy));
        }

        private bool HasNonProximityEnmityAsEnemyOutsidePair(uint enemy, uint pairedCreature)
        {
            return _enemyEnmityTables.TryGetValue(enemy, out var table) &&
                   table.Keys.Any(creature =>
                       creature != pairedCreature && HasNonProximityEnmity(creature, enemy));
        }

        /// <summary>
        /// Clears every creature from an enemy's enmity table.
        /// </summary>
        /// <param name="enemy">The enemy whose enmity table will be cleared.</param>
        public void ClearEnmityTable(uint enemy)
        {
            ClearEnmityTables(enemy);
        }

        /// <summary>
        /// Clears an enemy's enmity tables and removes associated creatures from cache.
        /// </summary>
        /// <param name="enemy">The enemy whose tables we're clearing</param>
        private void ClearEnmityTables(uint enemy)
        {
            _attackCommandTimes.Remove(enemy);
            _attackCommandTargets.Remove(enemy);

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
        public bool HasEnmity(uint creature)
        {
            return _creatureToEnemies.ContainsKey(creature)
                   && _creatureToEnemies[creature].Count > 0;
        }

        /// <summary>
        /// Resumes existing combat after the caller clears the action queue. The previous attack
        /// command is no longer pending, so its throttle must not delay the replacement command.
        /// </summary>
        public void ResumeAttackAfterActionsCleared(uint creature)
        {
            _attackCommandTimes.Remove(creature);
            _attackCommandTargets.Remove(creature);
            AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Forces a creature to attack the highest enmity target it can see.
        /// Stops an existing chase when its last proximity target is no longer in range.
        /// Stops attacking, but keeps its enmity, while every target is hidden by invisibility or stealth.
        /// If creature did not have enmity, nothing will happen.
        /// If the creature is already actively attacking that target, nothing will happen.
        /// </summary>
        public void AttackHighestEnmityTarget(uint creature)
        {
            uint target;
            var removedProximityEnmity = false;
            if (IsRegisteredCompanion(creature))
            {
                target = PeekAuthorizedTarget(creature);
            }
            else
            {
                target = GetHighestEnmityAttackTarget(creature);
                while (GetIsObjectValid(target) && ShouldRemoveStaleProximityTarget(creature, target))
                {
                    RemoveProximityEnmity(target, creature);
                    removedProximityEnmity = true;
                    target = GetHighestEnmityAttackTarget(creature);
                }

                if (!GetIsObjectValid(target) && GetIsObjectValid(GetHighestEnmityTarget(creature)))
                {
                    HoldForHiddenTargets(creature);
                    return;
                }
            }

            if (removedProximityEnmity && !GetIsObjectValid(target))
            {
                _ai.StopCombatAfterProximityLoss(creature);
                return;
            }

            AttackTargetIfNeeded(creature, target);
        }

        /// <summary>
        /// Every creature on the enemy's enmity table is hidden by invisibility or stealth. Drops the enemy's
        /// attack or chase so it stops striking at a target it cannot see. The enmity table is kept,
        /// so the enemy resumes once one of its targets can be seen again.
        /// </summary>
        private void HoldForHiddenTargets(uint creature)
        {
            if (_activity.IsBusy(creature) || _ai.IsLeashEvading(creature))
                return;

            var currentAction = GetCurrentAction(creature);
            if (currentAction != ActionType.AttackObject &&
                currentAction != ActionType.MoveToPoint &&
                !GetIsObjectValid(GetAttackTarget(creature)))
            {
                return;
            }

            _attackCommandTimes.Remove(creature);
            _attackCommandTargets.Remove(creature);
            AssignCommand(creature, () => ClearAllActions(true));
        }

        private void AttackTargetIfNeeded(uint creature, uint target)
        {
            if (!GetIsObjectValid(target) ||
                GetArea(creature) != GetArea(target))
                return;

            if (_ai.TryStartCombatLeashEvade(creature, target))
                return;

            // Same target - no need to switch.
            var attackTarget = GetAttackTarget(creature);
            var currentAction = GetCurrentAction(creature);
            var isBusy = _activity.IsBusy(creature);
            var shouldRecoverStaleAttack = ShouldRecoverStaleAttack(
                creature,
                attackTarget,
                target,
                currentAction);
            _attackCommandTimes.TryGetValue(creature, out var commandTime);
            var commandIssuedAt = commandTime == default
                ? (DateTime?)null
                : commandTime;
            var pendingTarget = _attackCommandTargets.TryGetValue(creature, out var queuedTarget)
                ? queuedTarget : OBJECT_INVALID;
            var recoverySeconds = GetStaleAttackRecoverySeconds(creature);

            if (!ShouldIssueAttackCommand(
                    attackTarget,
                    target,
                    currentAction,
                    isBusy,
                    shouldRecoverStaleAttack,
                    Clock.UtcNow,
                    commandIssuedAt,
                    recoverySeconds,
                    pendingTarget))
            {
                return;
            }

            if (shouldRecoverStaleAttack)
                WriteAILog($"{GetName(creature)} recovered stale attack action against {GetName(target)}.");

            IssueAttackCommand(creature, target);
        }

        public void IssueAttackCommand(uint creature, uint target, bool clearActions = true)
        {
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(target) ||
                GetArea(creature) != GetArea(target))
            {
                return;
            }

            if (!CanIssueAttackCommand(creature, target))
                return;

            if (_ai.TryStartCombatLeashEvade(creature, target))
                return;

            _attackCommandTimes[creature] = Clock.UtcNow;
            _attackCommandTargets[creature] = target;
            AssignCommand(creature, () =>
            {
                if (!_attackCommandTargets.TryGetValue(creature, out var queuedTarget) || queuedTarget != target)
                    return;
                if (_ai.TryStartCombatLeashEvade(creature, target))
                    return;

                if (clearActions)
                    ClearAllActions(true);

                if (ShouldMoveIntoAttackRange(creature, target))
                    ActionMoveToObject(target, true, GetAttackMoveRange(creature));

                ActionDoCommand(() =>
                {
                    if (!_attackCommandTargets.TryGetValue(creature, out var pendingTarget) || pendingTarget != target ||
                        !GetIsObjectValid(target) || GetIsDead(target) || GetArea(creature) != GetArea(target))
                        return;
                    if (_ai.TryStartCombatLeashEvade(creature, target))
                        return;

                    // The target may have turned invisible or slipped into stealth during the approach.
                    if (IsHiddenFrom(creature, target))
                        return;

                    ActionAttack(target);
                });
            });
        }

        private bool ShouldIssueAttackCommand(
            uint attackTarget,
            uint desiredTarget,
            ActionType currentAction,
            bool isBusy,
            bool shouldRecoverStaleAttack,
            DateTime now,
            DateTime? commandIssuedAt,
            float recoverySeconds,
            uint pendingTarget)
        {
            if (isBusy)
                return false;

            if (shouldRecoverStaleAttack)
                return true;

            // The native attack target can still describe the previous swing while
            // an already-issued switch waits for its assigned callback.
            if (pendingTarget == desiredTarget && HasRecentAttackCommand(now, commandIssuedAt, recoverySeconds))
                return false;

            if (attackTarget != OBJECT_INVALID && attackTarget != desiredTarget)
                return true;

            // An approach can have a destination before GetAttackTarget is populated.
            if (pendingTarget != OBJECT_INVALID && pendingTarget != desiredTarget)
                return true;

            if (HasRecentAttackCommand(now, commandIssuedAt, recoverySeconds))
                return false;

            return currentAction != ActionType.AttackObject;
        }

        private bool HasRecentAttackCommand(DateTime now, DateTime? commandIssuedAt, float recoverySeconds)
        {
            return commandIssuedAt != null &&
                   (now - commandIssuedAt.Value).TotalSeconds < recoverySeconds;
        }

        private bool ShouldMoveIntoAttackRange(uint creature, uint target)
        {
            if (GetIsPC(creature) ||
                !GetIsObjectValid(target))
            {
                return false;
            }

            var skillType = GetEquippedWeaponSkillType(creature);
            var moveRange = GetWeaponEngagementRange(skillType);

            return ShouldMoveIntoAttackRange(GetDistanceBetween(creature, target), skillType, moveRange);
        }

        private bool ShouldMoveIntoAttackRange(float distance, SkillType skillType, float moveRange)
        {
            var threshold = IsRangedWeaponSkill(skillType)
                ? moveRange + AttackMoveRangeTolerance
                : MeleeAttackMoveThreshold;

            return distance > threshold;
        }

        private float GetAttackMoveRange(uint creature)
        {
            var skillType = GetEquippedWeaponSkillType(creature);
            return GetWeaponEngagementRange(skillType);
        }

        private bool ShouldRemoveStaleProximityTarget(uint enemy, uint target)
        {
            return HasOnlyProximityEnmity(target, enemy) &&
                   !_ai.IsInAggroRange(enemy, target);
        }

        private bool HasOnlyProximityEnmity(uint creature, uint enemy)
        {
            var rawAmount = GetRawEnmityAmount(creature, enemy);
            if (rawAmount <= 0)
                return false;

            var proximityAmount = GetProximityEnmityAmount(creature, enemy);
            return proximityAmount >= rawAmount;
        }

        private bool ShouldRecoverStaleAttack(
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
                Clock.UtcNow,
                commandIssuedAt,
                HasRecentAttackActivity(creature, recoverySeconds),
                recoverySeconds);
        }

        private bool ShouldRecoverStaleAttack(
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

        private float GetStaleAttackRecoverySeconds(uint creature)
        {
            var calculatedDelay = CalculateAttackDelay(creature);
            var effectiveDelay = CalculateEffectiveAttackDelay(calculatedDelay);

            return GetStaleAttackRecoverySeconds(effectiveDelay);
        }

        private float GetStaleAttackRecoverySeconds(int effectiveDelayMilliseconds)
        {
            // Attacks arrive in swings; fast delays resolve multiple attacks per swing,
            // so staleness is measured against the swing cadence rather than the per-attack delay.
            var swingDelaySeconds = CalculateAttackSwingDelay(effectiveDelayMilliseconds) / 1000f;

            return Math.Max(MinimumStaleAttackRecoverySeconds, swingDelaySeconds * 2f + 1f);
        }

        /// <summary>
        /// Retrieves all of the enmity table information for a given creature.
        /// </summary>
        /// <param name="creature">The creature whose tables will be retrieved</param>
        /// <returns>A dictionary of enmity values for a given creature.</returns>
        public Dictionary<uint, int> GetEnmityTowardsAllEnemies(uint creature)
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

        private int GetRawEnmityAmount(uint creature, uint enemy)
        {
            return _enemyEnmityTables.TryGetValue(enemy, out var table) &&
                   table.TryGetValue(creature, out var amount)
                ? amount
                : 0;
        }

        private int GetProximityEnmityAmount(uint creature, uint enemy)
        {
            return _proximityEnmityAmounts.TryGetValue(enemy, out var table) &&
                   table.TryGetValue(creature, out var amount)
                ? amount
                : 0;
        }

        private void RemoveEnmityTableEntry(uint creature, uint enemy)
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

        private void RemoveProximityEnmityTracking(uint creature, uint enemy)
        {
            if (!_proximityEnmityAmounts.TryGetValue(enemy, out var table))
                return;

            table.Remove(creature);
            if (table.Count <= 0)
            {
                _proximityEnmityAmounts.Remove(enemy);
            }
        }

        /// <summary>The unconverted <c>Stat</c> pipeline.</summary>
        protected virtual int GetStatAdjustment(uint creature, StatType stat) => _stat.Value.GetStatAdjustment(creature, stat);

        /// <summary>The unconverted <c>Stealth</c> service.</summary>
        protected virtual bool IsHiddenFrom(uint observer, uint target) => _stealth.IsHiddenFrom(observer, target);

        /// <summary>The unconverted <c>Droid</c> service.</summary>
        protected virtual bool IsDroid(uint creature) => _droid.Value.IsDroid(creature);

        /// <summary>The companion-control service.</summary>
        protected virtual bool IsRegisteredCompanion(uint creature) => _companionControl.IsRegisteredCompanion(creature);

        /// <summary>The companion-control service.</summary>
        protected virtual uint PeekAuthorizedTarget(uint companion) => _companionControl.PeekAuthorizedTarget(companion);

        /// <summary>The companion-control service.</summary>
        protected virtual bool CanIssueAttackCommand(uint companion, uint target) =>
            _companionControl.CanIssueAttackCommand(companion, target);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual SkillType GetEquippedWeaponSkillType(uint creature) => _queries.Value.GetEquippedWeaponSkillType(creature);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual float GetWeaponEngagementRange(SkillType skillType) => CombatRules.GetWeaponEngagementRange(skillType);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual bool IsRangedWeaponSkill(SkillType skillType) => CombatRules.IsRangedWeaponSkill(skillType);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual bool HasRecentAttackActivity(uint creature, float windowSeconds) =>
            _combatActivity.Value.HasRecentAttackActivity(creature, windowSeconds);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual int CalculateAttackDelay(uint attacker) => _attackTiming.Value.CalculateAttackDelay(attacker);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual int CalculateEffectiveAttackDelay(int attackerDelayMilliseconds) =>
            CombatRules.CalculateEffectiveAttackDelay(attackerDelayMilliseconds);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual int CalculateAttackSwingDelay(int effectiveDelayMilliseconds) =>
            CombatRules.CalculateAttackSwingDelay(effectiveDelayMilliseconds);

        /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
        protected virtual HashSet<IStatusEffect> GetAllStatusEffects(uint creature) =>
            _statusEffect.GetCreatureStatusEffects(creature).GetAllEffects();

        /// <summary>The unconverted <c>Log</c> sink.</summary>
        protected virtual void WriteAILog(string details) => Log.Write(LogGroup.AI, details);
    }
}
