using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The repeated-target and same-target pressure modifiers of <c>Combat</c>: the three repeated-target damage stacks
    /// (the weapon-skill stack, the melee Rundown stack and the ranged Sustained Fire stack) and Spotter's Rhythm, and the
    /// per-attacker tables they keep, which are this instance's own fields.
    /// </summary>
    public class CombatRepeatedTargetService : ICombatRepeatedTargetService
    {
        private readonly Dictionary<uint, RepeatedTargetDamageState> _repeatedTargetDamageStates = new();
        private readonly Dictionary<uint, MeleeRepeatedTargetStack> _meleeRepeatedTargetDamageStates = new();
        private readonly Dictionary<uint, RepeatedTargetDamageState> _rangedRepeatedTargetDamageStates = new();
        private readonly Dictionary<uint, SameTargetPressureState> _sameTargetPressureStates = new();

        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ICombatRateModifiersService _rateModifiers;

        public CombatRepeatedTargetService(
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            ICombatRateModifiersService rateModifiers)
        {
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _rateModifiers = rateModifiers;
        }

        private sealed class SameTargetPressureState
        {
            public uint Target { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime LastBuildHitAt { get; set; }
            public DateTime ReadyUntil { get; set; }
        }

        public int ApplyRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage,
            bool isAbilityDamage,
            out int flatBonus)
        {
            flatBonus = 0;
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageSkillType));
            var autoAttackOnly = _stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageAutoAttackOnly) > 0;
            var percentPerHit = _stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamagePercentPerHit);
            var maxPercent = _stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamagePercentMax);
            var bonusPerHit = _stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageBonusPerHit);
            var maxBonus = _stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageBonusMax);
            var durationSeconds = _stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageDurationSeconds);
            var hasPercentBonus = percentPerHit > 0 && maxPercent > 0;
            var hasFlatBonus = bonusPerHit > 0 && maxBonus > 0;
            if (autoAttackOnly && isAbilityDamage ||
                !SkillTypeMatches(skillType, requiredSkillType) ||
                (!hasPercentBonus && !hasFlatBonus))
            {
                _repeatedTargetDamageStates.Remove(attacker);
                return damage;
            }

            var now = Clock.UtcNow;
            if (!_repeatedTargetDamageStates.TryGetValue(attacker, out var state) ||
                state.Target != defender ||
                durationSeconds > 0 && (now - state.LastHit).TotalSeconds > durationSeconds)
            {
                state = new RepeatedTargetDamageState(defender);
            }

            var maxStacks = 1;
            if (hasPercentBonus)
                maxStacks = Math.Max(maxStacks, (int)Math.Ceiling(maxPercent / (float)percentPerHit));
            if (hasFlatBonus)
                maxStacks = Math.Max(maxStacks, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));

            state.Stacks = Math.Min(state.Stacks + 1, maxStacks);
            state.LastHit = now;
            _repeatedTargetDamageStates[attacker] = state;

            if (hasPercentBonus)
            {
                var adjustment = Math.Min(maxPercent, state.Stacks * percentPerHit);
                damage += (int)Math.Ceiling(damage * (adjustment / 100f));
            }

            if (hasFlatBonus)
            {
                flatBonus = Math.Min(maxBonus, state.Stacks * bonusPerHit);
            }

            return damage;
        }

        public int ApplyMeleeRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage,
            bool isAbilityDamage)
        {
            // Ability hits neither build nor break the auto-attack chain; only auto-attacks count.
            if (damage <= 0 || isAbilityDamage || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var bonusPerHit = _stat.GetStatAdjustment(attacker, StatType.MeleeRepeatedTargetDamageBonusPerHit);
            var maxBonus = _stat.GetStatAdjustment(attacker, StatType.MeleeRepeatedTargetDamageBonusMax);
            var statusEffectIcon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                attacker,
                StatType.MeleeRepeatedTargetDamageStatusEffectIcon));
            if (!IsWeaponSkillType(skillType) ||
                bonusPerHit <= 0 ||
                maxBonus <= 0)
            {
                _meleeRepeatedTargetDamageStates.Remove(attacker);
                MeleeRepeatedTargetDamageStatusEffect.Refresh(attacker, 0, statusEffectIcon);
                return damage;
            }

            if (!_meleeRepeatedTargetDamageStates.TryGetValue(attacker, out var state))
            {
                state = new MeleeRepeatedTargetStack(defender);
                _meleeRepeatedTargetDamageStates[attacker] = state;
            }

            var maxStacks = Math.Max(1, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));
            var stackRaised = state.RegisterHit(defender, maxStacks);
            var stackBonus = Math.Min(maxBonus, state.Stacks * bonusPerHit);

            MeleeRepeatedTargetDamageStatusEffect.Refresh(attacker, state.Stacks, statusEffectIcon);

            // Announce the stack only while it builds; hits at the cap stay silent.
            if (stackRaised && GetIsPC(attacker))
            {
                FloatingTextStringOnCreature(
                    ColorToken.Combat(MeleeRepeatedTargetStack.GetStackText(statusEffectIcon, state.Stacks, maxStacks, stackBonus)),
                    attacker,
                    false);
            }

            return damage + stackBonus;
        }

        /// <summary>
        /// Consecutive weapon hits build this independent damage stack with any weapon family.
        /// Ability hits count too, and stacks expire on their own timer.
        /// </summary>
        public int ApplyRangedRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var bonusPerHit = _stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageBonusPerHit);
            var maxBonus = _stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageBonusMax);
            var durationSeconds = _stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageDurationSeconds);
            if (bonusPerHit <= 0 ||
                maxBonus <= 0 ||
                durationSeconds <= 0)
            {
                ClearRangedRepeatedTargetDamageTracker(attacker);
                return damage;
            }

            // Non-weapon damage (device pulses, Force powers) neither builds nor breaks the
            // weapon-hit streak, so it leaves the stack untouched.
            if (!IsWeaponSkillType(skillType))
                return damage;

            var now = Clock.UtcNow;
            if (!_rangedRepeatedTargetDamageStates.TryGetValue(attacker, out var state) ||
                state.Target != defender ||
                durationSeconds > 0 && (now - state.LastHit).TotalSeconds > durationSeconds)
            {
                state = new RepeatedTargetDamageState(defender);
            }

            var maxStacks = Math.Max(1, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));
            state.Stacks = Math.Min(state.Stacks + 1, maxStacks);
            state.LastHit = now;
            _rangedRepeatedTargetDamageStates[attacker] = state;

            // Keep the stack visible and refresh its timer on every qualifying hit. The
            // floating text is deliberately emitted here, after the stack has been capped,
            // so it always reports the actual bonus that the next hit receives.
            var stackBonus = Math.Min(maxBonus, state.Stacks * bonusPerHit);
            _statusEffect.RemoveStatusEffect(attacker, typeof(SustainedFireStatusEffect), false);
            _statusEffectApplication.ApplyStatusEffect(
                attacker,
                attacker,
                new SustainedFireStatusEffect(state.Stacks, maxStacks, stackBonus),
                durationSeconds > 0 ? durationSeconds : 0f);
            if (GetIsPC(attacker))
            {
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Sustained Fire {state.Stacks}/{maxStacks} (+{stackBonus} DMG)"),
                    attacker,
                    false);
            }

            return damage + stackBonus;
        }

        public void ClearRangedRepeatedTargetDamageTracker(uint creature)
        {
            _rangedRepeatedTargetDamageStates.Remove(creature);
            _statusEffect.RemoveStatusEffect(creature, typeof(SustainedFireStatusEffect), false);
        }

        public void ApplySameTargetPressureDamageEffects(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return;

            var buildSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.SameTargetPressureBuildSkillType));
            var buildSeconds = _stat.GetStatAdjustment(attacker, StatType.SameTargetPressureBuildSeconds);
            var graceSeconds = _stat.GetStatAdjustment(attacker, StatType.SameTargetPressureGraceSeconds);
            var readyDurationSeconds = _stat.GetStatAdjustment(attacker, StatType.SameTargetPressureReadyDurationSeconds);
            var damageBonus = _stat.GetStatAdjustment(attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus);
            if (buildSeconds <= 0 ||
                graceSeconds <= 0 ||
                readyDurationSeconds <= 0 ||
                damageBonus <= 0)
            {
                return;
            }

            var now = Clock.UtcNow;
            if (_sameTargetPressureStates.TryGetValue(attacker, out var state) &&
                state.ReadyUntil != default)
            {
                if (IsSameTargetPressureReadyStateActive(attacker, state, now))
                    return;

                ClearSameTargetPressureState(attacker);
                state = null;
            }

            if (!IsWeaponSkillType(skillType))
                return;

            if (!SkillTypeMatchesOrGlobal(skillType, buildSkillType))
            {
                if (state != null && state.Target != defender)
                    ClearSameTargetPressureState(attacker);

                return;
            }

            if (state == null || state.Target != defender)
            {
                state = CreateSameTargetPressureBuildState(defender, now);
            }
            else if ((now - state.LastBuildHitAt).TotalSeconds > graceSeconds)
            {
                state = CreateSameTargetPressureBuildState(defender, now);
            }
            else
            {
                state.LastBuildHitAt = now;
            }

            if ((now - state.StartedAt).TotalSeconds >= buildSeconds)
            {
                ReadySameTargetPressure(attacker, state, now, readyDurationSeconds);
                return;
            }

            _sameTargetPressureStates[attacker] = state;
        }

        public int GetSameTargetPressureWeaponAbilityDamageBonus(
            uint attacker,
            uint target,
            SkillType skillType)
        {
            if (!IsWeaponSkillType(skillType) ||
                !TryGetReadySameTargetPressureState(attacker, target, out _))
            {
                return 0;
            }

            return Math.Max(0, _stat.GetStatAdjustment(attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus));
        }

        public void ConsumeSameTargetPressureWeaponAbilityDamageBonus(
            uint attacker,
            uint target,
            SkillType skillType,
            int damage)
        {
            if (damage <= 0 ||
                GetSameTargetPressureWeaponAbilityDamageBonus(attacker, target, skillType) <= 0)
            {
                return;
            }

            ClearSameTargetPressureState(attacker);

            if (GetIsPC(attacker))
            {
                var bonus = _stat.GetStatAdjustment(attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus);
                FloatingTextStringOnCreature(ColorToken.Combat($"Spotter's Rhythm (+{bonus} DMG)"), attacker, false);
            }
        }

        private SameTargetPressureState CreateSameTargetPressureBuildState(uint target, DateTime now)
        {
            return new SameTargetPressureState
            {
                Target = target,
                StartedAt = now,
                LastBuildHitAt = now,
                ReadyUntil = default
            };
        }

        private void ReadySameTargetPressure(
            uint attacker,
            SameTargetPressureState state,
            DateTime now,
            int readyDurationSeconds)
        {
            state.ReadyUntil = now.AddSeconds(readyDurationSeconds);
            _sameTargetPressureStates[attacker] = state;

            _statusEffectApplication.ApplyStatusEffect(
                attacker,
                attacker,
                typeof(SpottersRhythmStatusEffect),
                readyDurationSeconds);

            if (GetIsPC(attacker))
            {
                FloatingTextStringOnCreature(ColorToken.Combat("Spotter's Rhythm"), attacker, false);
            }
        }

        private bool TryGetReadySameTargetPressureState(
            uint attacker,
            uint target,
            out SameTargetPressureState state)
        {
            state = null;
            if (!_sameTargetPressureStates.TryGetValue(attacker, out var existing) ||
                existing.ReadyUntil == default)
            {
                return false;
            }

            var now = Clock.UtcNow;
            if (!IsSameTargetPressureReadyStateActive(attacker, existing, now))
            {
                ClearSameTargetPressureState(attacker);
                return false;
            }

            if (existing.Target != target)
                return false;

            state = existing;
            return true;
        }

        private bool IsSameTargetPressureReadyStateActive(
            uint attacker,
            SameTargetPressureState state,
            DateTime now)
        {
            return state.ReadyUntil > now &&
                   _statusEffect.HasStatusEffect(attacker, typeof(SpottersRhythmStatusEffect), attacker);
        }

        private void ClearSameTargetPressureState(uint attacker)
        {
            _sameTargetPressureStates.Remove(attacker);
            _statusEffect.RemoveStatusEffect(
                attacker,
                typeof(SpottersRhythmStatusEffect),
                attacker,
                false);
        }

        private sealed class RepeatedTargetDamageState
        {
            public uint Target { get; }
            public int Stacks { get; set; }
            public DateTime LastHit { get; set; }

            public RepeatedTargetDamageState(uint target)
            {
                Target = target;
                LastHit = Clock.UtcNow;
            }
        }

        public void ForgetDamageModifierState(uint creature)
        {
            _rateModifiers.ForgetAutoAttackCycleCriticalRateCount(creature);
            _repeatedTargetDamageStates.Remove(creature);
            _meleeRepeatedTargetDamageStates.Remove(creature);
            _rangedRepeatedTargetDamageStates.Remove(creature);
            ClearSameTargetPressureState(creature);
            foreach (var pressureState in _sameTargetPressureStates.Where(x => x.Value.Target == creature).Select(x => x.Key).ToList())
            {
                ClearSameTargetPressureState(pressureState);
            }
        }
    }
}
