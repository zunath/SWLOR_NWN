using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static partial class Combat
    {
        private static readonly Dictionary<(uint, StatType), DateTime> _statTriggerCooldowns = new();
        private static readonly Dictionary<uint, DamageDerivedHealingState> _damageDerivedHealingStates = new();
        private static readonly Dictionary<uint, RollingHealingBudget> _passiveDamageHealingBudgets = new();
        private static readonly Dictionary<uint, RollingHealingBudget> _defeatedEnemyHealingBudgets = new();
        private static readonly Dictionary<(uint Creature, AbilityDetail Ability), AbilityStaminaCostState> _abilityStaminaCosts = new();

        private sealed class AbilityStaminaCostState
        {
            public int Cost { get; init; }
            public DateTime SpentAt { get; init; }
            public int NonCriticalRangedAbilityStaminaCost { get; set; }
            public bool StaminaRestoreApplied { get; set; }
            public int HitStaminaRefunded { get; set; }
            public int DeferredImpactCount { get; set; }
        }

        public static IDisposable BeginDamageDerivedHealing(uint creature, uint target)
        {
            _damageDerivedHealingStates.TryGetValue(creature, out var state);
            if (state == null || state.Target != target)
            {
                state = new DamageDerivedHealingState
                {
                    Target = target,
                    TargetHPBeforeImpact = Ability.GetRemainingDamageTargetHP(creature, target),
                    Parent = state
                };
                _damageDerivedHealingStates[creature] = state;
            }

            state.Depth++;
            return new DamageDerivedHealingScope(creature);
        }

        /// <summary>
        /// Includes defensive HP granted during damage calculation before critical healing
        /// or queued damage can consume the hit's snapshot. Started healing remains frozen.
        /// </summary>
        private static void RefreshDamageDerivedHealingTargetHP(uint creature, uint target)
        {
            if (_damageDerivedHealingStates.TryGetValue(creature, out var state) &&
                state.Target == target && !state.Damage.HasValue)
            {
                state.TargetHPBeforeImpact = Ability.GetRemainingDamageTargetHP(creature, target);
            }
        }

        public static int ApplyDamageDerivedHealing(
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
            amount = Stat.ApplyHealingReceivedAdjustment(creature, amount, applyReceivedEffects: false);
            amount = CalculateCappedDamageDerivedHealingAmount(damage, state.HealingApplied, amount);
            amount = Stat.CalculateEffectiveHealingAmount(amount, ObjectPlugin.GetCurrentHitPoints(creature), maximumHP);

            if (isActivatedHealing)
            {
                var sequence = Ability.GetAbilityImpactSequence(creature);
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
            Stat.ApplyHealingReceivedEffects(creature, amount);
            using var recorderAttribution = CombatRecorder.BeginRiderAttribution(creature, CombatRecordSourceKind.Lifesteal);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
            return amount;
        }

        private static void EndDamageDerivedHealing(uint creature)
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
            private readonly uint _creature;
            private bool _disposed;

            public DamageDerivedHealingScope(uint creature)
            {
                _creature = creature;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                EndDamageDerivedHealing(_creature);
            }
        }

        private static bool DidCrossHPThreshold(uint creature, int damage, int thresholdPercent)
        {
            var maxHP = GetMaxHitPoints(creature);
            var currentHP = GetCurrentHitPoints(creature);
            if (maxHP <= 0 || currentHP <= 0)
                return false;

            var thresholdHP = maxHP * (thresholdPercent / 100f);
            var previousHP = currentHP + damage;
            return previousHP >= thresholdHP && currentHP < thresholdHP;
        }

        private static void HealFromMaxHP(uint creature, int percent)
        {
            if (percent <= 0)
                return;

            var maximumHP = GetMaxHitPoints(creature);
            var amount = GameMath.PercentOf(maximumHP, percent);
            amount = Stat.ApplyHealingReceivedAdjustment(creature, amount, applyReceivedEffects: false);
            amount = Stat.CalculateEffectiveHealingAmount(amount, ObjectPlugin.GetCurrentHitPoints(creature), maximumHP);
            amount = TakeCombatHealingBudget(_defeatedEnemyHealingBudgets, creature, maximumHP,
                MaximumDefeatedEnemyHealingMaxHPPercentPerWindow, amount);
            if (amount <= 0)
                return;

            Stat.ApplyHealingReceivedEffects(creature, amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
        }

        private static int TakeCombatHealingBudget(
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

        internal static bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds)
        {
            return TryUseStatTrigger(creature, statType, TimeSpan.FromSeconds(cooldownSeconds));
        }

        internal static bool TryUseStatTrigger(uint creature, StatType statType, TimeSpan cooldown)
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

        private static int GetHighResourceStatAdjustment(uint activator, StatType payload, StatType threshold, AbilityDetail ability)
        {
            var sources = Ability.GetAbilityImpactStatSources(activator, payload);
            var adjustment = 0;
            for (var index = 0; index < sources.Count; index++)
            {
                var source = sources[index];
                if (IsCurrentFPAndStaminaAbovePercent(activator, source[threshold], ability))
                    adjustment = checked(adjustment + source[payload]);
            }
            return adjustment;
        }

        private static int GetNonCriticalRangedAbilityStaminaCostFlatAdjustment(
            uint creature,
            AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true ||
                !IsRangedWeaponSkill(GetAbilitySkillType(creature, ability)))
            {
                return 0;
            }

            return Math.Max(0, Stat.GetStatAdjustment(
                creature,
                StatType.NonCriticalRangedAbilityStaminaCostFlatAdjustment));
        }

        // Stat-driven FP/STM riders go through these so combat recordings credit the StatType
        // that granted them instead of whatever ability or nothing happens to be running.
        private static int RestoreFPFromStat(uint creature, StatType trigger, int amount)
        {
            using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(creature, trigger);
            return Stat.RestoreFP(creature, amount);
        }

        private static int RestoreStaminaFromStat(uint creature, StatType trigger, int amount)
        {
            using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(creature, trigger);
            return Stat.RestoreStamina(creature, amount);
        }

        private static void ReduceFPFromStat(uint creature, StatType trigger, int amount)
        {
            using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(creature, trigger);
            Stat.ReduceFP(creature, amount);
        }

        private static void ReduceStaminaFromStat(uint creature, StatType trigger, int amount)
        {
            using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(creature, trigger);
            Stat.ReduceStamina(creature, amount);
        }

        private static int RestoreAbilityHitStaminaFromStat(uint creature, StatType trigger, int requested)
        {
            using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(creature, trigger);
            return RestoreAbilityHitStamina(creature, requested);
        }

        private static int RestoreAbilityHitStaminaFromStat(uint creature, StatType trigger, int requested, AbilityDetail ability)
        {
            using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(creature, trigger);
            return RestoreAbilityHitStamina(creature, ability, requested);
        }

        public static int RefundCriticalRangedAbilityStaminaCost(uint creature, AbilityDetail ability)
        {
            if (!TryGetAbilityStaminaCostState(creature, ability, out var state) ||
                state.NonCriticalRangedAbilityStaminaCost <= 0)
            {
                return 0;
            }

            var amount = state.NonCriticalRangedAbilityStaminaCost;
            state.NonCriticalRangedAbilityStaminaCost = 0;
            using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(
                creature, StatType.NonCriticalRangedAbilityStaminaCostFlatAdjustment);
            return Stat.RestoreStamina(creature, amount);
        }

        public static void ApplyAbilityStaminaCostFPRestore(uint creature, AbilityDetail ability, int staminaCost)
        {
            if (ability == null)
                return;

            var skillType = GetAbilitySkillType(creature, ability);
            TrackAbilityStaminaCost(creature, ability, staminaCost);
            if (staminaCost <= 0)
                return;

            var restoreSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityStaminaCostFPRestorePercentSkillType));
            var restorePercent = Stat.GetStatAdjustment(creature, StatType.AbilityStaminaCostFPRestorePercent);
            if (restorePercent <= 0 || !SkillTypeMatches(skillType, restoreSkillType))
                return;

            var amount = CalculateResourceRestoreFromCost(staminaCost, restorePercent);
            if (amount <= 0)
                return;

            if (RestoreFPFromStat(creature, StatType.AbilityStaminaCostFPRestorePercent, amount) > 0)
                ApplyAbilityRestoredFPEffects(creature);
        }

        private static void TrackAbilityStaminaCost(uint creature, AbilityDetail ability, int staminaCost)
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
            _abilityStaminaCosts[key] = new AbilityStaminaCostState
            {
                Cost = staminaCost - nonCriticalRangedAbilityStaminaCost,
                SpentAt = Clock.UtcNow,
                NonCriticalRangedAbilityStaminaCost = nonCriticalRangedAbilityStaminaCost
            };
        }

        private static bool TryGetAbilityStaminaCostState(
            uint creature,
            AbilityDetail ability,
            out AbilityStaminaCostState state)
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
        public static int GetAbilityStaminaPaid(uint creature, AbilityDetail ability) =>
            TryGetAbilityStaminaCostState(creature, ability, out var state)
                ? state.Cost + state.NonCriticalRangedAbilityStaminaCost : 0;

        /// <summary>Hit rewards share the actual activation's stamina spend, retaining at least
        /// one stamina of cost. FP-funded abilities and explicit recovery actions keep their payouts.</summary>
        public static int RestoreAbilityHitStamina(uint creature, AbilityDetail ability, int requested, bool sendFeedback = true,
            bool retainMinimumCost = true)
        {
            if (requested <= 0)
                return 0;

            if (ability?.IsHostileAbility != true ||
                !ability.Requirements.OfType<AbilityRequirementStamina>().Any())
                return Stat.RestoreStamina(creature, requested, sendFeedback: sendFeedback);

            if (!TryGetAbilityStaminaCostState(creature, ability, out var state))
                return 0;

            var amount = retainMinimumCost
                ? CalculateAbilityHitStaminaRestore(state.Cost, state.HitStaminaRefunded, requested)
                : Math.Clamp(requested, 0, Math.Max(0, state.Cost - Math.Max(0, state.HitStaminaRefunded)));
            state.HitStaminaRefunded += amount;
            return amount > 0 ? Stat.RestoreStamina(creature, amount, sendFeedback: sendFeedback) : 0;
        }

        private static int RestoreAbilityHitStamina(uint creature, int requested)
        {
            return RestoreAbilityHitStamina(creature, Ability.GetActiveAbilityImpactSummary(creature)?.Ability, requested);
        }

        public static int GetPaidAbilityStaminaCost(uint creature, AbilityDetail ability) =>
            TryGetAbilityStaminaCostState(creature, ability, out var state) ? state.Cost : 0;

        public static void DeferAbilityStaminaCostContext(uint creature, AbilityDetail ability)
        {
            if (TryGetAbilityStaminaCostState(creature, ability, out var state))
            {
                state.DeferredImpactCount++;
            }
        }

        public static void CompleteAbilityStaminaCostContext(uint creature, AbilityDetail ability)
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

        public static void CompleteDeferredAbilityStaminaCostContext(uint creature, AbilityDetail ability)
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

        public static void ApplyAbilityFPCostStaminaRestore(uint creature, AbilityDetail ability, int fpCost)
        {
            if (fpCost <= 0 || ability == null)
                return;

            var skillType = GetAbilitySkillType(creature, ability);
            var restoreSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityFPCostStaminaRestorePercentSkillType));
            var restorePercent = Stat.GetStatAdjustment(creature, StatType.AbilityFPCostStaminaRestorePercent);
            if (restorePercent <= 0 || !SkillTypeMatches(skillType, restoreSkillType))
                return;

            var amount = CalculateResourceRestoreFromCost(fpCost, restorePercent);
            if (amount > 0)
                RestoreStaminaFromStat(creature, StatType.AbilityFPCostStaminaRestorePercent, amount);
        }

        public static int GetNextAbilityFPCostAdjustment(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityFPCostAdjustmentSkillType,
                StatType.NextAbilityFPCostAdjustmentSkillType));

            return storedSkillType == skillType
                ? TemporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.NextAbilityFPCostAdjustment,
                    StatType.NextAbilityFPCostAdjustmentSkillType)
                : 0;
        }

        public static int ConsumeNextAbilityFPCostAdjustment(uint creature, SkillType skillType)
        {
            var adjustment = GetNextAbilityFPCostAdjustment(creature, skillType);
            if (adjustment == 0)
                return 0;

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityFPCostAdjustment,
                StatType.NextAbilityFPCostAdjustmentSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityFPCostAdjustmentSkillType,
                StatType.NextAbilityFPCostAdjustmentSkillType);

            return adjustment;
        }

        public static void ApplyAbilityRestoredFPEffects(uint activator)
        {
            var haste = Stat.GetStatAdjustment(activator, StatType.AbilityRestoredFPHastePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityRestoredFPHasteDurationSeconds);
            if (haste == 0 || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                activator,
                new EnergizedFormsHasteStatusEffect(haste),
                duration);
        }

        public static void ApplyFPRestoredEffects(uint creature)
        {
            var forceAttack = Stat.GetStatAdjustment(creature, StatType.RestoredFPForceAttackPercentAdjustment);
            var duration = Stat.GetStatAdjustment(creature, StatType.RestoredFPForceAttackDurationSeconds);
            if (forceAttack == 0 || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                creature,
                creature,
                new ForceLensForceAttackStatusEffect(forceAttack),
                duration);
        }

        public static void ApplyStaminaRestoredEffects(uint creature)
        {
            var attack = Stat.GetStatAdjustment(creature, StatType.RestoredStaminaAttackPercentAdjustment);
            var duration = Stat.GetStatAdjustment(creature, StatType.RestoredStaminaAttackDurationSeconds);
            if (attack == 0 || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                creature,
                creature,
                new ForceLensAttackStatusEffect(attack),
                duration);
        }

        public static void ApplyAbilityRestoredBothResourcesEffects(uint activator)
        {
            var haste = Stat.GetStatAdjustment(activator, StatType.AbilityRestoredBothResourcesHastePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityRestoredBothResourcesHasteDurationSeconds);
            if (haste == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.AttackDelayReductionPercent,
                haste,
                duration,
                StatType.AbilityRestoredBothResourcesHastePercentAdjustment);
        }

        private static bool IsCurrentFPAtOrAbovePercent(uint creature, int thresholdPercent)
        {
            var maxFP = Stat.GetMaxFP(creature);
            if (maxFP <= 0)
                return false;

            return Stat.GetCurrentFP(creature) >= maxFP * (thresholdPercent / 100f);
        }

        public static bool IsCurrentFPAndStaminaAbovePercent(
            uint creature,
            int thresholdPercent,
            AbilityDetail ability = null)
        {
            if (thresholdPercent <= 0)
                return false;

            var maxFP = Stat.GetMaxFP(creature);
            var maxStamina = Stat.GetMaxStamina(creature);
            if (maxFP <= 0 || maxStamina <= 0)
                return false;

            var currentStamina = Stat.GetCurrentStamina(creature);
            if (TryGetAbilityStaminaCostState(creature, ability, out var costState))
            {
                currentStamina += costState.NonCriticalRangedAbilityStaminaCost;
            }

            return Stat.GetCurrentFP(creature) > maxFP * (thresholdPercent / 100f) &&
                   currentStamina > maxStamina * (thresholdPercent / 100f);
        }

        public static bool IsCurrentFPAndStaminaAtOrBelowPercent(uint creature, int thresholdPercent)
        {
            if (thresholdPercent <= 0)
                return false;

            var maxFP = Stat.GetMaxFP(creature);
            var maxStamina = Stat.GetMaxStamina(creature);
            if (maxFP <= 0 || maxStamina <= 0)
                return false;

            return Stat.GetCurrentFP(creature) <= maxFP * (thresholdPercent / 100f) &&
                   Stat.GetCurrentStamina(creature) <= maxStamina * (thresholdPercent / 100f);
        }

        private static void ForgetResourceTriggerState(uint creature)
        {
            _passiveDamageHealingBudgets.Remove(creature);
            _defeatedEnemyHealingBudgets.Remove(creature);
            foreach (var key in _statTriggerCooldowns.Keys.Where(x => x.Item1 == creature).ToList())
            {
                _statTriggerCooldowns.Remove(key);
            }
        }

        private static void ForgetAbilityStaminaCostState(uint creature)
        {
            foreach (var key in _abilityStaminaCosts.Keys.Where(x => x.Creature == creature).ToList())
            {
                _abilityStaminaCosts.Remove(key);
            }
        }
    }
}
