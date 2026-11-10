using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The FP, stamina and healing bookkeeping of <c>Combat</c>: stat trigger cooldowns, damage-derived healing and its
    /// budgets, the stamina each hostile ability paid and what its hits may refund, and the riders that restored FP or
    /// stamina grant. The five tables are this instance's own fields.
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
        private readonly IAbilityService _ability;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;

        public CombatResourcesService(
            ICombatQueriesService queries,
            IAbilityService ability,
            IStatService stat,
            IStatusEffectService statusEffect,
            ICombatRecorderService combatRecorder,
            ITemporaryStatModifierService temporaryStatModifier)
        {
            _queries = queries;
            _ability = ability;
            _stat = stat;
            _statusEffect = statusEffect;
            _combatRecorder = combatRecorder;
            _temporaryStatModifier = temporaryStatModifier;
        }

        public IDisposable BeginDamageDerivedHealing(uint creature, uint target)
        {
            _damageDerivedHealingStates.TryGetValue(creature, out var state);
            if (state == null || state.Target != target)
            {
                state = new DamageDerivedHealingState
                {
                    Target = target,
                    TargetHPBeforeImpact = _ability.GetRemainingDamageTargetHP(creature, target),
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
                state.TargetHPBeforeImpact = _ability.GetRemainingDamageTargetHP(creature, target);
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
            amount = _stat.ApplyHealingReceivedAdjustment(creature, amount, applyReceivedEffects: false);
            amount = CalculateCappedDamageDerivedHealingAmount(damage, state.HealingApplied, amount);
            amount = _stat.CalculateEffectiveHealingAmount(amount, ObjectPlugin.GetCurrentHitPoints(creature), maximumHP);

            if (isActivatedHealing)
            {
                var sequence = _ability.GetAbilityImpactSequence(creature);
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
            _stat.ApplyHealingReceivedEffects(creature, amount);
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
            amount = _stat.ApplyHealingReceivedAdjustment(creature, amount, applyReceivedEffects: false);
            amount = _stat.CalculateEffectiveHealingAmount(amount, ObjectPlugin.GetCurrentHitPoints(creature), maximumHP);
            amount = TakeCombatHealingBudget(_defeatedEnemyHealingBudgets, creature, maximumHP,
                MaximumDefeatedEnemyHealingMaxHPPercentPerWindow, amount);
            if (amount <= 0)
                return;

            _stat.ApplyHealingReceivedEffects(creature, amount);
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

        public bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds)
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
            var sources = _ability.GetAbilityImpactStatSources(activator, payload);
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

            if ((Clock.UtcNow - state.SpentAt).TotalSeconds <= 35)
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
            return RestoreAbilityHitStamina(creature, _ability.GetActiveAbilityImpactSummary(creature)?.Ability, requested);
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

        public void ApplyFPRestoredEffects(uint creature)
        {
            var forceAttack = _stat.GetStatAdjustment(creature, StatType.RestoredFPForceAttackPercentAdjustment);
            var duration = _stat.GetStatAdjustment(creature, StatType.RestoredFPForceAttackDurationSeconds);
            if (forceAttack == 0 || duration <= 0)
                return;

            _statusEffect.ApplyStatusEffect(
                creature,
                creature,
                new ForceLensForceAttackStatusEffect(forceAttack),
                duration);
        }

        public void ApplyStaminaRestoredEffects(uint creature)
        {
            var attack = _stat.GetStatAdjustment(creature, StatType.RestoredStaminaAttackPercentAdjustment);
            var duration = _stat.GetStatAdjustment(creature, StatType.RestoredStaminaAttackDurationSeconds);
            if (attack == 0 || duration <= 0)
                return;

            _statusEffect.ApplyStatusEffect(
                creature,
                creature,
                new ForceLensAttackStatusEffect(attack),
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
    }
}
