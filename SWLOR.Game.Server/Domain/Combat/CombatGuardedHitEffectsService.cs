using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The effects a guarded hit grants the guard: the remembered hit, the next attack and next skill ability counters and
    /// the Guard Reversal exposed rider with its consumption.
    /// </summary>
    public class CombatGuardedHitEffectsService : ICombatGuardedHitEffectsService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly IStatService _stat;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly ICombatDefenseMemoryService _defenseMemory;

        public CombatGuardedHitEffectsService(
            ICombatQueriesService queries,
            ICombatAttackTimingService attackTiming,
            IStatService stat,
            ITemporaryStatModifierService temporaryStatModifier,
            ICombatDefenseMemoryService defenseMemory)
        {
            _queries = queries;
            _attackTiming = attackTiming;
            _stat = stat;
            _temporaryStatModifier = temporaryStatModifier;
            _defenseMemory = defenseMemory;
        }

        public void TrackGuardedHit(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            _defenseMemory.NoteGuardedHit(creature);
            ApplyGuardedHitNextAttackEffects(creature);
            ApplyGuardedHitNextSkillAbilityEffects(creature);
            ApplyGuardedHitNextSkillAbilityStatusEffects(creature);
        }

        public void ApplyGuardedHitNextAttackEffects(uint creature)
        {
            var primaryDMGBonus = _stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextAttackDMGBonus);
            var criticalRate = _stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextAttackCriticalRatePercentAdjustment);
            var primaryWindow = _stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextAttackWindowSeconds);
            var secondaryDMGBonus = _stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitSecondaryNextAttackDMGBonus);
            var enmityBonus = _stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitSecondaryNextAttackEnmityBonus);
            var secondaryWindow = _stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitSecondaryNextAttackWindowSeconds);
            var dmgBonus = primaryDMGBonus + secondaryDMGBonus;
            var window = Math.Max(primaryWindow, secondaryWindow);
            if (window <= 0 || dmgBonus == 0 && criticalRate == 0 && enmityBonus == 0)
                return;

            _temporaryStatModifier.Replace(
                creature,
                StatType.NextAttackGuardedHitDMGBonus,
                dmgBonus,
                window,
                StatType.NextAttackGuardedHitDMGBonus);
            _temporaryStatModifier.Replace(
                creature,
                StatType.NextAttackGuardedHitCriticalRatePercentAdjustment,
                criticalRate,
                window,
                StatType.NextAttackGuardedHitDMGBonus);
            _temporaryStatModifier.Replace(
                creature,
                StatType.NextAttackGuardedHitEnmityBonus,
                enmityBonus,
                window,
                StatType.NextAttackGuardedHitDMGBonus);

            if (GetIsPC(creature))
            {
                var criticalText = criticalRate != 0
                    ? $", +{criticalRate}% Crit"
                    : string.Empty;
                var enmityText = enmityBonus != 0
                    ? $", +{enmityBonus} Enmity"
                    : string.Empty;
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Counter Ready: +{dmgBonus} DMG{criticalText}{enmityText}"),
                    creature,
                    false);
            }
        }

        private void ApplyGuardedHitNextSkillAbilityEffects(uint creature)
        {
            var primary = GetGuardedHitNextSkillAbilityBonuses(
                creature,
                StatType.GuardedHitNextSkillAbilitySkillType,
                StatType.GuardedHitNextSkillAbilityDamageBonus,
                StatType.GuardedHitNextSkillAbilityCriticalRatePercentAdjustment,
                StatType.GuardedHitNextSkillAbilityWindowSeconds);
            var secondary = GetGuardedHitNextSkillAbilityBonuses(
                creature,
                StatType.GuardedHitSecondaryNextSkillAbilitySkillType,
                StatType.GuardedHitSecondaryNextSkillAbilityDamageBonus,
                StatType.GuardedHitSecondaryNextSkillAbilityCriticalRatePercentAdjustment,
                StatType.GuardedHitSecondaryNextSkillAbilityWindowSeconds);

            var selected = primary;
            if (primary.SkillType == SkillType.Invalid)
            {
                selected = secondary;
            }
            // Guarded-hit bonuses sharing a skill are one window and intentionally stack their payloads.
            // Keeping their selector/window providers independent prevents integer selector values and
            // durations from being accidentally added by _stat.GetStatAdjustment.
            else if (secondary.SkillType == SkillType.Invalid || secondary.SkillType == primary.SkillType)
            {
                selected = (
                    primary.SkillType,
                    primary.DamageBonus + secondary.DamageBonus,
                    primary.CriticalRate + secondary.CriticalRate,
                    Math.Max(primary.Window, secondary.Window));
            }
            else
            {
                // Different weapon selectors cannot both be consumed by one "next skill ability" slot.
                // Prefer the currently equipped weapon's channel; otherwise preserve the primary channel.
                var equippedSkillType = _queries.GetEquippedWeaponSkillType(creature);
                if (secondary.SkillType == equippedSkillType)
                    selected = secondary;
            }

            _attackTiming.GrantNextSkillAbilityBonuses(
                creature,
                selected.SkillType,
                selected.DamageBonus,
                selected.CriticalRate,
                selected.Window);

            if (selected.SkillType != SkillType.Invalid &&
                selected.Window > 0 &&
                (selected.DamageBonus != 0 || selected.CriticalRate != 0) &&
                GetIsPC(creature))
            {
                var criticalText = selected.CriticalRate != 0
                    ? $", +{selected.CriticalRate}% Crit"
                    : string.Empty;
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Counter Ready: +{selected.DamageBonus} DMG{criticalText}"),
                    creature,
                    false);
            }
        }

        private (SkillType SkillType, int DamageBonus, int CriticalRate, int Window)
            GetGuardedHitNextSkillAbilityBonuses(
            uint creature,
            StatType skillTypeStat,
            StatType damageBonusStat,
            StatType criticalRateStat,
            StatType windowStat)
        {
            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(creature, skillTypeStat));
            var criticalRate = _stat.GetStatAdjustment(creature, criticalRateStat);
            var damageBonus = _stat.GetStatAdjustment(creature, damageBonusStat);
            var window = _stat.GetStatAdjustment(creature, windowStat);
            return (skillType, damageBonus, criticalRate, window);
        }

        private void ApplyGuardedHitNextSkillAbilityStatusEffects(uint creature)
        {
            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextSkillAbilityStatusSkillType));
            var duration = _stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            var damageBonus = _stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityExposedDamageBonus);
            if (skillType == SkillType.Invalid || duration <= 0 && damageBonus <= 0)
                return;

            var wasArmed = _temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextSkillAbilityExposedDamageBonus,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds) != 0;
            _attackTiming.ShowReadyCue(creature, wasArmed, damageBonus, ReadyCue.Damage("Guard Reversal", damageBonus));

            var window = Math.Max(1, duration);
            _temporaryStatModifier.Replace(
                creature,
                StatType.GuardedHitNextSkillAbilityStatusSkillType,
                (int)skillType,
                window,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);

            if (duration > 0)
            {
                _temporaryStatModifier.Replace(
                    creature,
                    StatType.GuardedHitNextSkillAbilityExposedDurationSeconds,
                    duration,
                    window,
                    StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            }

            if (damageBonus > 0)
            {
                _temporaryStatModifier.Replace(
                    creature,
                    StatType.GuardedHitNextSkillAbilityExposedDamageBonus,
                    damageBonus,
                    window,
                    StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            }
        }

        public int ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(uint creature, SkillType skillType)
        {
            var storedSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextSkillAbilityStatusSkillType,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds));
            if (!SkillTypeMatches(skillType, storedSkillType))
                return 0;

            return _temporaryStatModifier.Consume(
                creature,
                StatType.GuardedHitNextSkillAbilityExposedDamageBonus,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
        }
    }
}
