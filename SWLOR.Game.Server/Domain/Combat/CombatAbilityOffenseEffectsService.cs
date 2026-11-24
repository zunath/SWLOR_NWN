using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The offense riders of an ability activation: the movement speed, the area ability rewards, the hostile ability Force
    /// Attack, attack adjustment, sequence and resource restore riders, and the next auto-attack damage bonus. Every step is a
    /// public member that <see cref="CombatAbilityActivationService"/> calls in its fixed order. The hostile ability sequence
    /// per activator is this instance's own table, forgotten by <see cref="ForgetActivationState"/>.
    /// </summary>
    public class CombatAbilityOffenseEffectsService : ICombatAbilityOffenseEffectsService
    {
        private readonly Dictionary<uint, HostileAbilitySequenceState> _hostileAbilitySequenceStates = new();

        private sealed class HostileAbilitySequenceState
        {
            public FeatType LastFeat { get; init; }
            public DateTime LastUse { get; init; }
        }

        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatDamageTakenEffectsService _damageTakenEffects;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;

        public CombatAbilityOffenseEffectsService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatDamageTakenEffectsService damageTakenEffects,
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier)
        {
            _queries = queries;
            _resources = resources;
            _damageTakenEffects = damageTakenEffects;
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
        }

        public void ApplyAreaAbilityUsedRewards(uint activator, AbilityDetail ability, bool isAreaAbility)
        {
            if (ability?.IsHostileAbility != true || !isAreaAbility)
                return;

            var grantedDeflection = false;
            foreach (var source in _stat.GetStatSources(activator, StatType.AreaAbilityUsedAttackDeflection))
            {
                var deflection = source[StatType.AreaAbilityUsedAttackDeflection];
                var duration = source[StatType.AreaAbilityUsedAttackDeflectionDurationSeconds];
                if (deflection <= 0 || duration <= 0)
                    continue;

                _temporaryStatModifier.Replace(activator,
                    _stat.GetGrantedDeflectionStatType(StatType.AreaAbilityUsedAttackDeflection),
                    deflection, duration, source.GetModifierGroup(StatType.AreaAbilityUsedAttackDeflection));
                grantedDeflection = true;
            }
            if (grantedDeflection)
                _damageTakenEffects.ApplyAbilityGrantedAttackDeflectionEffects(activator,
                    _stat.GetStatTypeDeflectionSource(StatType.AreaAbilityUsedAttackDeflection));

            var restore = _stat.GetStatAdjustment(activator, StatType.AreaAbilityUsedFPRestore);
            if (restore > 0 && _resources.RestoreFPFromStat(activator, StatType.AreaAbilityUsedFPRestore, restore) > 0)
                _resources.ApplyAbilityRestoredFPEffects(activator);
        }

        public void ApplyAbilityUsedMovementSpeed(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            if (!_queries.AbilityUsedTriggerSkillMatches(
                    activator,
                    ability,
                    skillType,
                    StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType,
                    StatType.AbilityUsedMovementSpeedRangedOnly))
            {
                return;
            }

            var movementSpeed = _stat.GetStatAdjustment(activator, StatType.AbilityUsedMovementSpeedPercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityUsedMovementSpeedDurationSeconds);
            if (movementSpeed == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                activator,
                StatType.MovementSpeedPercentAdjustment,
                movementSpeed,
                duration,
                StatType.AbilityUsedMovementSpeedPercentAdjustment);
        }

        public void ApplyHostileAbilityForceAttack(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var forceAttack = _stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackPercentPerStack);
            var duration = _stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackDurationSeconds);
            var maximum = _stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackPercentMax);
            if (forceAttack == 0 || duration <= 0 || maximum <= 0)
                return;

            var current = _statusEffect.GetStatusEffect(
                activator,
                typeof(HostileAbilityForceAttackStatusEffect)) as HostileAbilityForceAttackStatusEffect;
            var total = Math.Min(maximum, (current?.ForceAttack ?? 0) + forceAttack);

            _statusEffectApplication.ApplyStatusEffect(
                activator,
                activator,
                new HostileAbilityForceAttackStatusEffect(total),
                duration);
        }

        /// <summary>
        /// Grants stacking Force Attack when the activated hostile ability costs at least the
        /// configured minimum FP. Drives the Lightsaber Severance "Overpower" trait.
        /// </summary>
        public void ApplyHostileAbilityFPSpendForceAttack(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var forceAttack = _stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackPercent);
            var duration = _stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackDurationSeconds);
            var maximum = _stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackMaxPercent);
            var minimumFPCost = _stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackMinFPCost);
            if (forceAttack <= 0 || duration <= 0 || maximum <= 0 || minimumFPCost <= 0)
                return;

            var fpCost = ability.Requirements
                .OfType<AbilityRequirementFP>()
                .Sum(x => x.RequiredFP);
            if (fpCost < minimumFPCost)
                return;

            _temporaryStatModifier.AddCapped(
                activator,
                StatType.ForceAttackPercentAdjustment,
                forceAttack,
                duration,
                maximum,
                StatType.HostileAbilityFPSpendForceAttackPercent,
                1,
                refreshExistingStacks: true);
        }

        public void ApplyHostileAbilityUsedAttackAdjustment(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var attack = _stat.GetStatAdjustment(activator, StatType.HostileAbilityUsedAttackPercentAdjustment);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityUsedAttackPercentAdjustmentDurationSeconds);
            var maximum = _stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityUsedAttackPercentAdjustmentMaximum);
            if (attack <= 0 || duration <= 0 || maximum <= 0)
                return;

            _temporaryStatModifier.AddCapped(
                activator,
                StatType.AttackPercentAdjustment,
                attack,
                duration,
                maximum,
                StatType.HostileAbilityUsedAttackPercentAdjustment,
                1,
                refreshExistingStacks: true);

            var currentTotal = _temporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.AttackPercentAdjustment,
                StatType.HostileAbilityUsedAttackPercentAdjustment);
            if (currentTotal > 0)
            {
                _statusEffectApplication.ApplyStatusEffect(
                    activator,
                    activator,
                    new ButchersTempoStatusEffect(currentTotal),
                    duration);
            }
        }

        public void ApplyHostileAbilitySequenceEffects(
            uint activator,
            FeatType feat,
            AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null || !ability.IsHostileAbility)
                return;

            var windowSeconds = _stat.GetStatAdjustment(activator, StatType.HostileAbilitySequenceWindowSeconds);
            var bleedDuration = _stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds);
            if (windowSeconds <= 0 || bleedDuration <= 0)
            {
                _hostileAbilitySequenceStates[activator] = new HostileAbilitySequenceState
                {
                    LastFeat = feat,
                    LastUse = Clock.UtcNow
                };
                return;
            }

            var now = Clock.UtcNow;
            if (_hostileAbilitySequenceStates.TryGetValue(activator, out var state) &&
                state.LastFeat != feat &&
                (now - state.LastUse).TotalSeconds <= windowSeconds)
            {
                _temporaryStatModifier.Replace(
                    activator,
                    StatType.NextDamageDealtBleedDurationSeconds,
                    bleedDuration,
                    windowSeconds,
                    StatType.NextDamageDealtBleedDurationSeconds);
            }

            _hostileAbilitySequenceStates[activator] = new HostileAbilitySequenceState
            {
                LastFeat = feat,
                LastUse = now
            };
        }

        public void ApplyHostileAbilityResourceRestoreEffects(uint activator, AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var fpRestore = _stat.GetStatAdjustment(activator, StatType.HostileAbilityFPRestore);
            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.HostileAbilityStaminaRestore);

            var restoredFP = fpRestore > 0
                ? _resources.RestoreFPFromStat(activator, StatType.HostileAbilityFPRestore, fpRestore)
                : 0;
            if (restoredFP > 0)
                _resources.ApplyAbilityRestoredFPEffects(activator);

            var restoredStamina = staminaRestore > 0
                ? _resources.RestoreStaminaFromStat(activator, StatType.HostileAbilityStaminaRestore, staminaRestore)
                : 0;

            if (restoredFP > 0 && restoredStamina > 0)
                _resources.ApplyAbilityRestoredBothResourcesEffects(activator);
        }

        public void ApplyNextAutoAttackDamageBonus(
            uint activator,
            StatType bonusStatType,
            StatType durationStatType)
        {
            var bonus = _stat.GetStatAdjustment(activator, bonusStatType);
            var duration = _stat.GetStatAdjustment(activator, durationStatType);
            if (bonus == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                activator,
                StatType.NextAutoAttackDamageBonus,
                bonus,
                duration,
                StatType.NextAutoAttackDamageBonus);
        }

        public void ForgetActivationState(uint creature)
        {
            _hostileAbilitySequenceStates.Remove(creature);
        }
    }
}
