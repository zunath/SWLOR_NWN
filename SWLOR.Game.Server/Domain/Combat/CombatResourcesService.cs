using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The FP, stamina and healing bookkeeping of <c>Combat</c>: stat trigger cooldowns, damage-derived healing and its
    /// budgets, the stamina each hostile ability paid and what its hits may refund, and the healing adjustments and what a
    /// received heal sets off (part 7 of the stat split: moved from <c>StatService</c> unchanged except that the stat
    /// read and the pools are <see cref="IStatService"/> calls). The five tables are this instance's own fields. The riders
    /// that restored FP or stamina grant are <c>ResourceRestoredEffectsService</c>'s (part 8).
    /// </summary>
    public class CombatResourcesService : ICombatResourcesService
    {
        public const int MaximumActivatedDamageHealingMaxHPPercent = 15;
        public const int MaximumPassiveDamageHealingMaxHPPercentPerHit = 6;
        public const int MaximumCooldownDamageHealingMaxHPPercentPerHit = 12;
        public const int MaximumPassiveDamageHealingMaxHPPercentPerWindow = 24;
        public const int MaximumDefeatedEnemyHealingMaxHPPercentPerWindow = 12;
        public const int CombatHealingWindowSeconds = 6;

        private readonly Dictionary<(uint, StatType), DateTime> _statTriggerCooldowns = new();
        private readonly Dictionary<uint, DamageDerivedHealingState> _damageDerivedHealingStates = new();
        private readonly Dictionary<uint, RollingHealingBudget> _passiveDamageHealingBudgets = new();
        private readonly Dictionary<uint, RollingHealingBudget> _defeatedEnemyHealingBudgets = new();
        private readonly Dictionary<(uint Creature, AbilityDetail Ability), CombatAbilityStaminaCostState> _abilityStaminaCosts = new();

        private readonly ICombatQueriesService _queries;
        private readonly IAbilityImpactState _impactState;
        private readonly IStatService _stat;
        private readonly IStatusEffectApplicationService _statusEffect;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IPvpCombatService _pvpCombat;
        private readonly ICompanionLookup _companions;

        public CombatResourcesService(
            ICombatQueriesService queries,
            IAbilityImpactState impactState,
            IStatService stat,
            IStatusEffectApplicationService statusEffect,
            ICombatRecorderService combatRecorder,
            ITemporaryStatModifierService temporaryStatModifier,
            IPvpCombatService pvpCombat,
            ICompanionLookup companions)
        {
            _queries = queries;
            _impactState = impactState;
            _stat = stat;
            _statusEffect = statusEffect;
            _combatRecorder = combatRecorder;
            _temporaryStatModifier = temporaryStatModifier;
            _pvpCombat = pvpCombat;
            _companions = companions;
        }

        public IDisposable BeginDamageDerivedHealing(uint creature, uint target)
        {
            _damageDerivedHealingStates.TryGetValue(creature, out var state);
            if (state == null || state.Target != target)
            {
                state = new DamageDerivedHealingState
                {
                    Target = target,
                    TargetHPBeforeImpact = _impactState.GetRemainingDamageTargetHP(creature, target),
                    Parent = state
                };
                _damageDerivedHealingStates[creature] = state;
            }

            state.Depth++;
            return new DamageDerivedHealingScope(this, creature);
        }

        /// <summary>
        /// Includes defensive HP granted during damage calculation before critical healing
        /// or queued damage can consume the hit's snapshot. Started healing remains frozen.
        /// </summary>
        public void RefreshDamageDerivedHealingTargetHP(uint creature, uint target)
        {
            if (_damageDerivedHealingStates.TryGetValue(creature, out var state) &&
                state.Target == target && !state.Damage.HasValue)
            {
                state.TargetHPBeforeImpact = _impactState.GetRemainingDamageTargetHP(creature, target);
            }
        }

        public int ApplyDamageDerivedHealing(
            uint creature,
            int damage,
            int percent,
            bool isActivatedHealing = false,
            int passiveHealingCooldownSeconds = 0)
        {
            if (damage <= 0 || percent <= 0 ||
                !_damageDerivedHealingStates.TryGetValue(creature, out var state))
                return 0;

            state.Damage ??= CalculateDamageEligibleForHealing(damage, state.TargetHPBeforeImpact);
            damage = state.Damage.Value;
            if (damage <= 0)
                return 0;

            var maximumHP = GetMaxHitPoints(creature);
            var amount = GameMath.PercentOf(damage, percent);
            amount = ApplyHealingReceivedAdjustment(creature, amount, applyReceivedEffects: false);
            amount = CalculateCappedDamageDerivedHealingAmount(damage, state.HealingApplied, amount);
            amount = CalculateEffectiveHealingAmount(amount, ObjectPlugin.GetCurrentHitPoints(creature), maximumHP);

            if (isActivatedHealing)
            {
                var sequence = _impactState.GetAbilityImpactSequence(creature);
                amount = sequence != null
                    ? sequence.TakeDamageDerivedHealing(maximumHP, amount)
                    : Math.Min(amount, Math.Max(0, CalculateMaxHPHealingBudget(
                        maximumHP, MaximumActivatedDamageHealingMaxHPPercent) - state.ActivatedHealingApplied));
                state.ActivatedHealingApplied += amount;
            }
            else
            {
                var isCooldownHealing = passiveHealingCooldownSeconds > 0;
                var maximumPercent = isCooldownHealing
                    ? MaximumCooldownDamageHealingMaxHPPercentPerHit
                    : MaximumPassiveDamageHealingMaxHPPercentPerHit;
                var healingApplied = isCooldownHealing ? state.CooldownHealingApplied : state.PassiveHealingApplied;
                amount = Math.Min(amount, Math.Max(0, CalculateMaxHPHealingBudget(
                    maximumHP, maximumPercent) - healingApplied));
                amount = TakeCombatHealingBudget(_passiveDamageHealingBudgets, creature, maximumHP,
                    MaximumPassiveDamageHealingMaxHPPercentPerWindow, amount);
                if (isCooldownHealing)
                    state.CooldownHealingApplied += amount;
                else
                    state.PassiveHealingApplied += amount;
            }

            if (amount <= 0)
                return 0;

            state.HealingApplied += amount;
            ApplyHealingReceivedEffects(creature, amount);
            using var recorderAttribution = _combatRecorder.BeginRiderAttribution(creature, CombatRecordSourceKind.Lifesteal);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
            return amount;
        }

        private void EndDamageDerivedHealing(uint creature)
        {
            if (!_damageDerivedHealingStates.TryGetValue(creature, out var state))
                return;

            state.Depth--;
            if (state.Depth <= 0)
            {
                if (state.Parent == null)
                    _damageDerivedHealingStates.Remove(creature);
                else
                    _damageDerivedHealingStates[creature] = state.Parent;
            }
        }

        private sealed class DamageDerivedHealingState
        {
            public int Depth { get; set; }
            public uint Target { get; set; }
            public int TargetHPBeforeImpact { get; set; }
            public DamageDerivedHealingState Parent { get; set; }
            public int? Damage { get; set; }
            public int HealingApplied { get; set; }
            public int PassiveHealingApplied { get; set; }
            public int CooldownHealingApplied { get; set; }
            public int ActivatedHealingApplied { get; set; }
        }

        private sealed class DamageDerivedHealingScope : IDisposable
        {
            private readonly CombatResourcesService _resources;
            private readonly uint _creature;
            private bool _disposed;

            public DamageDerivedHealingScope(CombatResourcesService resources, uint creature)
            {
                _resources = resources;
                _creature = creature;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _resources.EndDamageDerivedHealing(_creature);
            }
        }

        public bool DidCrossHPThreshold(uint creature, int damage, int thresholdPercent)
        {
            var maxHP = GetMaxHitPoints(creature);
            var currentHP = GetCurrentHitPoints(creature);
            if (maxHP <= 0 || currentHP <= 0)
                return false;

            var thresholdHP = maxHP * (thresholdPercent / 100f);
            var previousHP = currentHP + damage;
            return previousHP >= thresholdHP && currentHP < thresholdHP;
        }

        public void HealFromMaxHP(uint creature, int percent)
        {
            if (percent <= 0)
                return;

            var maximumHP = GetMaxHitPoints(creature);
            var amount = GameMath.PercentOf(maximumHP, percent);
            amount = ApplyHealingReceivedAdjustment(creature, amount, applyReceivedEffects: false);
            amount = CalculateEffectiveHealingAmount(amount, ObjectPlugin.GetCurrentHitPoints(creature), maximumHP);
            amount = TakeCombatHealingBudget(_defeatedEnemyHealingBudgets, creature, maximumHP,
                MaximumDefeatedEnemyHealingMaxHPPercentPerWindow, amount);
            if (amount <= 0)
                return;

            ApplyHealingReceivedEffects(creature, amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
        }

        private int TakeCombatHealingBudget(
            Dictionary<uint, RollingHealingBudget> budgets, uint creature, int maximumHP, int percent, int requested)
        {
            if (requested <= 0)
                return 0;

            if (!budgets.TryGetValue(creature, out var budget))
            {
                budget = new RollingHealingBudget(TimeSpan.FromSeconds(CombatHealingWindowSeconds));
                budgets[creature] = budget;
            }

            return budget.Take(CalculateMaxHPHealingBudget(maximumHP, percent), requested, Clock.UtcNow);
        }

        public int ApplyOutgoingAbilityHealingAdjustment(uint source, int amount)
        {
            if (amount <= 0 || !GetIsObjectValid(source))
                return amount;

            var statSource = _companions.IsPlayerBeast(source)
                ? GetMaster(source)
                : source;
            var adjustment = _stat.GetStatAdjustment(
                statSource,
                StatType.OutgoingAbilityHealingPercentAdjustment);

            return CalculateOutgoingAbilityHealingAmount(amount, adjustment);
        }

        public int CalculateOutgoingAbilityHealingAmount(int amount, int adjustment)
        {
            if (amount <= 0 || adjustment <= 0)
                return amount;

            return amount + (int)Math.Ceiling(amount * (adjustment / 100f));
        }

        /// <param name="isInstantAbilityHeal">True for a heal an ability lands when it resolves. Only these can restore Stamina through the healing-received trigger.</param>
        public int ApplyHealingReceivedAdjustment(uint creature, int amount, bool applyReceivedEffects = true, bool isInstantAbilityHeal = false)
        {
            if (amount <= 0)
                return amount;

            var adjustment = _stat.GetStatAdjustment(creature, StatType.HealingReceivedPercentAdjustment);
            var adjustedAmount = Math.Max(1, StatRules.ApplyPercentAdjustment(amount, adjustment));

            if (applyReceivedEffects)
                ApplyHealingReceivedEffects(creature, adjustedAmount, isInstantAbilityHeal);

            return adjustedAmount;
        }

        public int CalculateEffectiveHealingAmount(int amount, int currentHP, int maxHP)
        {
            return currentHP <= 0 ? 0 : Math.Clamp(amount, 0, Math.Max(0, maxHP - currentHP));
        }

        /// <param name="isInstantAbilityHeal">Heals over time, lifesteal, drains and regeneration pass false and never restore Stamina.</param>
        public void ApplyHealingReceivedEffects(uint creature, int amount, bool isInstantAbilityHeal = false)
        {
            // The PvP healing ramp cuts the heal when it lands, so these react to what is left of it.
            amount = PvpCombatRules.ReduceHealing(amount, _pvpCombat.GetHealingReductionPercent(creature));
            if (CalculateEffectiveHealingAmount(amount, ObjectPlugin.GetCurrentHitPoints(creature), GetMaxHitPoints(creature)) <= 0)
                return;
            // The heal these react to is applied right after them.
            using var recorderReaction = _combatRecorder.BeginHealingReaction(creature);
            if (isInstantAbilityHeal)
                ApplyHealingReceivedStaminaRestore(creature);
            ApplyHealingReceivedAttackBoost(creature);
        }

        private void ApplyHealingReceivedStaminaRestore(uint creature)
        {
            var chance = _stat.GetStatAdjustment(creature, StatType.HealingReceivedStaminaRestoreChance);
            var stamina = _stat.GetStatAdjustment(creature, StatType.HealingReceivedStaminaRestore);
            if (chance <= 0 || stamina <= 0)
                return;

            var maximumChance = _stat.GetStatAdjustment(creature, StatType.HealingReceivedStaminaRestoreChanceMaximum);
            var scalingAbility = GetAbilityTypeFromStatValue(_stat.GetStatAdjustment(creature, StatType.HealingReceivedStaminaRestoreChanceScalingAbility));
            if (maximumChance > chance && scalingAbility != AbilityType.Invalid)
            {
                chance = Math.Min(maximumChance, chance + Math.Max(0, GetAbilityScore(creature, scalingAbility)));
            }

            if (Domain.Foundation.Random.D100(1) > chance)
                return;

            var cooldownSeconds = _stat.GetStatAdjustment(creature, StatType.HealingReceivedStaminaRestoreCooldownSeconds);
            if (!TryUseStatTrigger(creature, StatType.HealingReceivedStaminaRestore, cooldownSeconds))
                return;

            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creature, StatType.HealingReceivedStaminaRestore);
            _stat.RestoreStamina(creature, stamina);
        }

        private void ApplyHealingReceivedAttackBoost(uint creature)
        {
            var attackPercent = _stat.GetStatAdjustment(creature, StatType.HealingReceivedAttackPercentAdjustment);
            var duration = _stat.GetStatAdjustment(creature, StatType.HealingReceivedAttackDurationSeconds);
            if (attackPercent == 0 || duration <= 0)
                return;

            ReplaceTemporaryStatModifier(
                creature,
                StatType.AttackPercentAdjustment,
                attackPercent,
                duration,
                StatType.HealingReceivedAttackPercentAdjustment);
        }

        private AbilityType GetAbilityTypeFromStatValue(int value)
        {
            var abilityValue = value - 1;
            return Enum.IsDefined(typeof(AbilityType), abilityValue)
                ? (AbilityType)abilityValue
                : AbilityType.Invalid;
        }

        // Virtual so the healing tests' double answers the gate, as the stat service's seam did before the healing moved here.
        public virtual bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds)
        {
            return TryUseStatTrigger(creature, statType, TimeSpan.FromSeconds(cooldownSeconds));
        }

        public bool TryUseStatTrigger(uint creature, StatType statType, TimeSpan cooldown)
        {
            if (cooldown <= TimeSpan.Zero)
                return true;

            var key = (creature, statType);
            var now = Clock.UtcNow;
            if (_statTriggerCooldowns.TryGetValue(key, out var nextAvailable) && nextAvailable > now)
                return false;

            _statTriggerCooldowns[key] = now.Add(cooldown);
            return true;
        }

        public int GetHighResourceStatAdjustment(uint activator, StatType payload, StatType threshold, AbilityDetail ability)
        {
            var sources = _impactState.GetAbilityImpactStatSources(activator, payload, _stat);
            var adjustment = 0;
            for (var index = 0; index < sources.Count; index++)
            {
                var source = sources[index];
                if (IsCurrentFPAndStaminaAbovePercent(activator, source[threshold], ability))
                    adjustment = checked(adjustment + source[payload]);
            }
            return adjustment;
        }

        public int GetNonCriticalRangedAbilityStaminaCostFlatAdjustment(
            uint creature,
            AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true ||
                !IsRangedWeaponSkill(_queries.GetAbilitySkillType(creature, ability)))
            {
                return 0;
            }

            return Math.Max(0, _stat.GetStatAdjustment(
                creature,
                StatType.NonCriticalRangedAbilityStaminaCostFlatAdjustment));
        }

        // Stat-driven FP/STM riders go through these so combat recordings credit the StatType
        // that granted them instead of whatever ability or nothing happens to be running.
        public int RestoreFPFromStat(uint creature, StatType trigger, int amount)
        {
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creature, trigger);
            return _stat.RestoreFP(creature, amount);
        }

        public int RestoreStaminaFromStat(uint creature, StatType trigger, int amount)
        {
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creature, trigger);
            return _stat.RestoreStamina(creature, amount);
        }

        public void ReduceFPFromStat(uint creature, StatType trigger, int amount)
        {
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creature, trigger);
            _stat.ReduceFP(creature, amount);
        }

        public void ReduceStaminaFromStat(uint creature, StatType trigger, int amount)
        {
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creature, trigger);
            _stat.ReduceStamina(creature, amount);
        }

        public int RestoreAbilityHitStaminaFromStat(uint creature, StatType trigger, int requested)
        {
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creature, trigger);
            return RestoreAbilityHitStamina(creature, requested);
        }

        public int RestoreAbilityHitStaminaFromStat(uint creature, StatType trigger, int requested, AbilityDetail ability)
        {
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creature, trigger);
            return RestoreAbilityHitStamina(creature, ability, requested);
        }

        public int RefundCriticalRangedAbilityStaminaCost(uint creature, AbilityDetail ability)
        {
            if (!TryGetAbilityStaminaCostState(creature, ability, out var state) ||
                state.NonCriticalRangedAbilityStaminaCost <= 0)
            {
                return 0;
            }

            var amount = state.NonCriticalRangedAbilityStaminaCost;
            state.NonCriticalRangedAbilityStaminaCost = 0;
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(
                creature, StatType.NonCriticalRangedAbilityStaminaCostFlatAdjustment);
            return _stat.RestoreStamina(creature, amount);
        }

        public void ApplyAbilityStaminaCostFPRestore(uint creature, AbilityDetail ability, int staminaCost)
        {
            if (ability == null)
                return;

            var skillType = _queries.GetAbilitySkillType(creature, ability);
            TrackAbilityStaminaCost(creature, ability, staminaCost);
            if (staminaCost <= 0)
                return;

            var restoreSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.AbilityStaminaCostFPRestorePercentSkillType));
            var restorePercent = _stat.GetStatAdjustment(creature, StatType.AbilityStaminaCostFPRestorePercent);
            if (restorePercent <= 0 || !SkillTypeMatches(skillType, restoreSkillType))
                return;

            var amount = CalculateResourceRestoreFromCost(staminaCost, restorePercent);
            if (amount <= 0)
                return;

            if (RestoreFPFromStat(creature, StatType.AbilityStaminaCostFPRestorePercent, amount) > 0)
                ApplyAbilityRestoredFPEffects(creature);
        }

        private void TrackAbilityStaminaCost(uint creature, AbilityDetail ability, int staminaCost)
        {
            if (!GetIsObjectValid(creature) ||
                ability?.IsHostileAbility != true)
            {
                return;
            }

            var key = (creature, ability);
            if (staminaCost <= 0)
            {
                _abilityStaminaCosts.Remove(key);
                return;
            }

            var nonCriticalRangedAbilityStaminaCost = Math.Min(
                staminaCost,
                GetNonCriticalRangedAbilityStaminaCostFlatAdjustment(creature, ability));
            _abilityStaminaCosts[key] = new CombatAbilityStaminaCostState
            {
                Cost = staminaCost - nonCriticalRangedAbilityStaminaCost,
                SpentAt = Clock.UtcNow,
                NonCriticalRangedAbilityStaminaCost = nonCriticalRangedAbilityStaminaCost
            };
        }

        public bool TryGetAbilityStaminaCostState(
            uint creature,
            AbilityDetail ability,
            out CombatAbilityStaminaCostState state)
        {
            state = null;
            if (ability == null)
                return false;

            var key = (creature, ability);
            if (!_abilityStaminaCosts.TryGetValue(key, out state))
                return false;

            if (state.ScheduledImpactDepth > 0 || (Clock.UtcNow - state.SpentAt).TotalSeconds <= 35)
                return true;

            _abilityStaminaCosts.Remove(key);
            state = null;
            return false;
        }

        /// <summary>The cost paid by this activation, before any hit refunds.</summary>
        public int GetAbilityStaminaPaid(uint creature, AbilityDetail ability) =>
            TryGetAbilityStaminaCostState(creature, ability, out var state)
                ? state.Cost + state.NonCriticalRangedAbilityStaminaCost : 0;

        /// <summary>Hit rewards share the actual activation's stamina spend, retaining at least
        /// one stamina of cost. FP-funded abilities and explicit recovery actions keep their payouts.</summary>
        public int RestoreAbilityHitStamina(uint creature, AbilityDetail ability, int requested, bool sendFeedback = true,
            bool retainMinimumCost = true)
        {
            if (requested <= 0)
                return 0;

            if (ability?.IsHostileAbility != true ||
                !ability.Requirements.OfType<AbilityRequirementStamina>().Any())
                return _stat.RestoreStamina(creature, requested, sendFeedback: sendFeedback);

            if (!TryGetAbilityStaminaCostState(creature, ability, out var state))
                return 0;

            var amount = retainMinimumCost
                ? CalculateAbilityHitStaminaRestore(state.Cost, state.HitStaminaRefunded, requested)
                : Math.Clamp(requested, 0, Math.Max(0, state.Cost - Math.Max(0, state.HitStaminaRefunded)));
            state.HitStaminaRefunded += amount;
            return amount > 0 ? _stat.RestoreStamina(creature, amount, sendFeedback: sendFeedback) : 0;
        }

        private int RestoreAbilityHitStamina(uint creature, int requested)
        {
            return RestoreAbilityHitStamina(creature, _impactState.GetActiveAbilityImpactSummary(creature)?.Ability, requested);
        }

        public int GetPaidAbilityStaminaCost(uint creature, AbilityDetail ability) =>
            TryGetAbilityStaminaCostState(creature, ability, out var state) ? state.Cost : 0;

        public void DeferAbilityStaminaCostContext(uint creature, AbilityDetail ability)
        {
            if (TryGetAbilityStaminaCostState(creature, ability, out var state))
            {
                state.DeferredImpactCount++;
            }
        }

        /// <summary>
        /// Captures this activation's stamina cost context for impacts that land after the cast ends, such as
        /// scheduled device pulses. Returns a wrapper that runs an impact with that activation's context in place,
        /// so its hit rewards share the stamina the activation paid and a later activation of the same ability
        /// cannot be charged for them. Returns null when the activation paid no stamina.
        /// </summary>
        public Action<Action> CaptureAbilityStaminaCostContext(uint creature, AbilityDetail ability)
        {
            if (!TryGetAbilityStaminaCostState(creature, ability, out var state))
                return null;

            var key = (creature, ability);
            return impact =>
            {
                _abilityStaminaCosts.TryGetValue(key, out var current);
                _abilityStaminaCosts[key] = state;
                state.ScheduledImpactDepth++;
                try
                {
                    impact();
                }
                finally
                {
                    state.ScheduledImpactDepth--;
                    if (current == null)
                        _abilityStaminaCosts.Remove(key);
                    else
                        _abilityStaminaCosts[key] = current;
                }
            };
        }

        public void CompleteAbilityStaminaCostContext(uint creature, AbilityDetail ability)
        {
            if (ability == null)
                return;

            if (_abilityStaminaCosts.TryGetValue((creature, ability), out var state) &&
                state.DeferredImpactCount > 0)
            {
                return;
            }

            _abilityStaminaCosts.Remove((creature, ability));
        }

        public void CompleteDeferredAbilityStaminaCostContext(uint creature, AbilityDetail ability)
        {
            if (ability == null ||
                !_abilityStaminaCosts.TryGetValue((creature, ability), out var state))
            {
                return;
            }

            state.DeferredImpactCount = Math.Max(0, state.DeferredImpactCount - 1);
            if (state.DeferredImpactCount == 0)
            {
                _abilityStaminaCosts.Remove((creature, ability));
            }
        }

        public void ApplyAbilityFPCostStaminaRestore(uint creature, AbilityDetail ability, int fpCost)
        {
            if (fpCost <= 0 || ability == null)
                return;

            var skillType = _queries.GetAbilitySkillType(creature, ability);
            var restoreSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.AbilityFPCostStaminaRestorePercentSkillType));
            var restorePercent = _stat.GetStatAdjustment(creature, StatType.AbilityFPCostStaminaRestorePercent);
            if (restorePercent <= 0 || !SkillTypeMatches(skillType, restoreSkillType))
                return;

            var amount = CalculateResourceRestoreFromCost(fpCost, restorePercent);
            if (amount > 0)
                RestoreStaminaFromStat(creature, StatType.AbilityFPCostStaminaRestorePercent, amount);
        }

        public int GetNextAbilityFPCostAdjustment(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityFPCostAdjustmentSkillType,
                StatType.NextAbilityFPCostAdjustmentSkillType));

            return storedSkillType == skillType
                ? _temporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.NextAbilityFPCostAdjustment,
                    StatType.NextAbilityFPCostAdjustmentSkillType)
                : 0;
        }

        public int ConsumeNextAbilityFPCostAdjustment(uint creature, SkillType skillType)
        {
            var adjustment = GetNextAbilityFPCostAdjustment(creature, skillType);
            if (adjustment == 0)
                return 0;

            _temporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityFPCostAdjustment,
                StatType.NextAbilityFPCostAdjustmentSkillType);
            _temporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityFPCostAdjustmentSkillType,
                StatType.NextAbilityFPCostAdjustmentSkillType);

            return adjustment;
        }

        public void ApplyAbilityRestoredFPEffects(uint activator)
        {
            var haste = _stat.GetStatAdjustment(activator, StatType.AbilityRestoredFPHastePercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityRestoredFPHasteDurationSeconds);
            if (haste == 0 || duration <= 0)
                return;

            _statusEffect.ApplyStatusEffect(
                activator,
                activator,
                new EnergizedFormsHasteStatusEffect(haste),
                duration);
        }

        public void ApplyAbilityRestoredBothResourcesEffects(uint activator)
        {
            var haste = _stat.GetStatAdjustment(activator, StatType.AbilityRestoredBothResourcesHastePercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityRestoredBothResourcesHasteDurationSeconds);
            if (haste == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                activator,
                StatType.AttackDelayReductionPercent,
                haste,
                duration,
                StatType.AbilityRestoredBothResourcesHastePercentAdjustment);
        }

        public bool IsCurrentFPAtOrAbovePercent(uint creature, int thresholdPercent)
        {
            var maxFP = _stat.GetMaxFP(creature);
            if (maxFP <= 0)
                return false;

            return _stat.GetCurrentFP(creature) >= maxFP * (thresholdPercent / 100f);
        }

        public bool IsCurrentFPAndStaminaAbovePercent(
            uint creature,
            int thresholdPercent,
            AbilityDetail ability = null)
        {
            if (thresholdPercent <= 0)
                return false;

            var maxFP = _stat.GetMaxFP(creature);
            var maxStamina = _stat.GetMaxStamina(creature);
            if (maxFP <= 0 || maxStamina <= 0)
                return false;

            var currentStamina = _stat.GetCurrentStamina(creature);
            if (TryGetAbilityStaminaCostState(creature, ability, out var costState))
            {
                currentStamina += costState.NonCriticalRangedAbilityStaminaCost;
            }

            return _stat.GetCurrentFP(creature) > maxFP * (thresholdPercent / 100f) &&
                   currentStamina > maxStamina * (thresholdPercent / 100f);
        }

        public bool IsCurrentFPAndStaminaAtOrBelowPercent(uint creature, int thresholdPercent)
        {
            if (thresholdPercent <= 0)
                return false;

            var maxFP = _stat.GetMaxFP(creature);
            var maxStamina = _stat.GetMaxStamina(creature);
            if (maxFP <= 0 || maxStamina <= 0)
                return false;

            return _stat.GetCurrentFP(creature) <= maxFP * (thresholdPercent / 100f) &&
                   _stat.GetCurrentStamina(creature) <= maxStamina * (thresholdPercent / 100f);
        }

        public void ForgetResourceTriggerState(uint creature)
        {
            _passiveDamageHealingBudgets.Remove(creature);
            _defeatedEnemyHealingBudgets.Remove(creature);
            foreach (var key in _statTriggerCooldowns.Keys.Where(x => x.Item1 == creature).ToList())
            {
                _statTriggerCooldowns.Remove(key);
            }
        }

        public void ForgetAbilityStaminaCostState(uint creature)
        {
            foreach (var key in _abilityStaminaCosts.Keys.Where(x => x.Creature == creature).ToList())
            {
                _abilityStaminaCosts.Remove(key);
            }
        }

        protected virtual void ReplaceTemporaryStatModifier(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType) =>
            _temporaryStatModifier.Replace(creature, statType, amount, durationSeconds, groupStatType);
    }
}
