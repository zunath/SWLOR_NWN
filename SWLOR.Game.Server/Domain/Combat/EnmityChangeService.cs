using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The changes to enmity and the event handlers that cause them: damage, healing and attacks raise it, taunts catch it up,
    /// reductions and proximity move it, and death, exit, destroy and limbo remove it. The logic that used to be static in
    /// <c>Service.Enmity</c> is moved here unchanged; the tables are written only through <see cref="IEnmityService"/> and each change
    /// asks <see cref="IEnmityAttackService"/> for a new attack target. The event handlers run on the container's instance; static
    /// callers reach it through <c>GameServices.EnmityChange</c>. Calls into the unconverted hot core and feature code sit behind
    /// one-line <c>protected virtual</c> methods whose bodies are the original calls.
    /// </summary>
    public class EnmityChangeService : IEnmityChangeService
    {
        private readonly IEnmityService _table;
        private readonly IEnmityAttackService _attack;
        private readonly ICombatLeash _leash;
        private readonly IHologramQuery _hologram;
        private readonly IPartyRoster _party;
        private readonly ICompanionLookup _companions;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;

        public EnmityChangeService(
            IEnmityService table,
            IEnmityAttackService attack,
            ICombatLeash leash,
            IHologramQuery hologram,
            IPartyRoster party,
            ICompanionLookup companions,
            IStatService stat,
            IStatusEffectService statusEffect)
        {
            _table = table;
            _attack = attack;
            _leash = leash;
            _hologram = hologram;
            _party = party;
            _companions = companions;
            _stat = stat;
            _statusEffect = statusEffect;
        }

        public const int MinimumEnmityPercentAdjustment = -50;
        public const int MaximumEnmityPercentAdjustment = 50;

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
            foreach (var enemy in _table.GetEnmityTowardsAllEnemies(target).Keys)
            {
                if (_table.HasNonProximityEnmity(target, enemy))
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
            _table.ClearEnmityTables(enemy);
            RemoveCreatureEnmity(enemy);
        }

        /// <summary>
        /// When a creature is destroyed with DestroyObject, remove all enmity tables it is associated with.
        /// </summary>
        [NWNEventHandler(ScriptName.OnObjectDestroyed)]
        public void CreatureDestroyed()
        {
            var enemy = OBJECT_SELF;
            _table.ClearEnmityTables(enemy);
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
        [PlayerSession(PlayerReload.LogoutOnly,
            "Makes enemies drop the player; a reload carries the enmity tables.")]
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
                    _table.ClearEnmityTables(target);
                    RemoveCreatureEnmity(target);
                }
            }
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

            if (_hologram.IsHologram(creature) || _hologram.IsHologram(enemy))
                return;

            if (CreatureAIRules.IsLeashEvading(enemy))
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

            if (_leash.TryStartCombatLeashEvade(enemy, creature))
                return;

            // Percent adjustment from feats/effects.
            if (adjustGeneration)
                amount = CalculateAdjustedEnmity(amount, CalculateEnmityAdjustment(creature, enemy));

            // Put the creature on the enemy's table, raise its enmity and list the enemy among its enemies.
            _table.AddEnmity(creature, enemy, amount);

            _attack.AttackHighestEnmityTarget(enemy);

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
                GetIsObjectValid(GetMaster(enemy)) || _companions.IsPlayerBeast(enemy) || IsDroid(enemy) ||
                GetArea(creature) != GetArea(enemy) || !GetIsReactionTypeHostile(enemy, creature) ||
                !LineOfSightObject(creature, enemy) || CreatureAIRules.IsLeashEvading(enemy) ||
                _leash.TryStartCombatLeashEvade(enemy, creature))
                return false;

            var rival = _table.GetEnmityTable(enemy)
                .Where(entry => entry.Key != creature && _attack.IsEligibleAttackTarget(enemy, entry.Key))
                .Select(entry => entry.Value).DefaultIfEmpty(0).Max();
            var desired = CalculateTauntEnmity(_table.GetRawEnmityAmount(creature, enemy), rival,
                CalculateAdjustedEnmity(bonus, CalculateEnmityAdjustment(creature, enemy)));
            if (desired > _table.GetRawEnmityAmount(creature, enemy))
            {
                // Register through the ordinary path without scaling the catch-up twice.
                ModifyEnmity(creature, enemy, desired - _table.GetRawEnmityAmount(creature, enemy), false);
                if (!_table.GetEnmityTable(enemy).ContainsKey(creature))
                    return false;
            }
            else
                _attack.AttackHighestEnmityTarget(enemy);
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

            foreach (var enemy in _table.GetEnemiesOf(creature))
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

            foreach (var enemy in _table.GetEnemiesOf(creature))
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
            if (!_table.ReduceEnmityEntry(creature, enemy, percent))
                return;

            _attack.AttackHighestEnmityTarget(enemy);
            ExecuteScript(ScriptName.OnEnmityChanged, creature);
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

            var previousAmount = _table.GetRawEnmityAmount(creature, enemy);
            ModifyEnmity(creature, enemy, amount);
            var currentAmount = _table.GetRawEnmityAmount(creature, enemy);
            var appliedAmount = currentAmount - previousAmount;
            if (appliedAmount <= 0)
                return;

            _table.AddProximityEnmity(creature, enemy, appliedAmount);
        }

        /// <summary>
        /// Removes a creature from all enmity tables.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        public void RemoveCreatureEnmity(uint creature)
        {
            _table.ForgetAttackCommand(creature);

            // Retrieve all of the creatures who have this creature on their enmity table.
            var enemies = _table.GetEnemiesOf(creature);
            foreach (var enemy in enemies)
            {
                _table.RemoveEnmityTableEntry(creature, enemy);
                _table.RemoveProximityEnmityTracking(creature, enemy);
                _attack.AttackHighestEnmityTarget(enemy);
            }
        }

        /// <summary>The unconverted <c>Stat</c> pipeline.</summary>
        protected virtual int GetStatAdjustment(uint creature, StatType stat) => _stat.GetStatAdjustment(creature, stat);

        /// <summary>The unconverted <c>Droid</c> service.</summary>
        protected virtual bool IsDroid(uint creature) => _companions.IsDroid(creature);

        /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
        protected virtual HashSet<IStatusEffect> GetAllStatusEffects(uint creature) =>
            _statusEffect.GetCreatureStatusEffects(creature).GetAllEffects();
    }
}
