using NWN.Native.API;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What a deflection sets off: part 6 of the stat split (<c>Readmes/MaintainabilityRefactorStatSplit.md</c>), moved
    /// from <c>StatService</c> unchanged except for how it reaches its dependencies. The stat read and the pools it called on its
    /// own instance are <see cref="IStatService"/> calls, and the Combat services it reached through <c>Lazy&lt;&gt;</c> are
    /// plain parameters. <see cref="ApplyDeflectionEffectsNative"/> tracks the deflection, reads every payload into a
    /// <see cref="DeflectionReaction"/> and then applies them, in the order it always had. Static callers reach it through
    /// <c>GameServices.CombatDeflectionReaction</c>. Its calls into the other Combat services and the temporary stat modifiers sit
    /// behind one-line <c>protected virtual</c> methods whose bodies are the original calls.
    /// </summary>
    public class CombatDeflectionReactionService : ICombatDeflectionReactionService
    {
        private const float DeflectionEvasionBoostDurationSeconds = 30f;
        private const float DeflectionEnmityBoostDurationSeconds = 30f;
        private const float DeflectionDefenseBoostDurationSeconds = 30f;

        private readonly ICharacterRecording _combatRecorder;
        private readonly IRecastService _recast;
        private readonly ICombatDamageTakenEffectsService _damageTakenEffects;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly ICombatResourcesService _resources;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IStatService _stat;

        public CombatDeflectionReactionService(
            ICharacterRecording combatRecorder,
            IRecastService recast,
            ICombatDamageTakenEffectsService damageTakenEffects,
            ICombatAttackTimingService attackTiming,
            ICombatResourcesService resources,
            ITemporaryStatModifierService temporaryStatModifier,
            IStatService stat)
        {
            _combatRecorder = combatRecorder;
            _recast = recast;
            _damageTakenEffects = damageTakenEffects;
            _attackTiming = attackTiming;
            _resources = resources;
            _temporaryStatModifier = temporaryStatModifier;
            _stat = stat;
        }

        public void ApplyDeflectionEffectsNative(CNWSCreature creature, DeflectionSource source)
        {
            var creatureId = GetNativeCreatureId(creature);
            TrackDeflection(creatureId, source);

            ReadDeflectionReaction(creatureId, source, out var reaction);
            var staminaRestoreStat = reaction.StaminaRestoreStat;
            var staminaRestore = reaction.StaminaRestore;
            var fpRestoreStat = reaction.FPRestoreStat;
            var fpRestore = reaction.FPRestore;
            var staminaRestorePercent = reaction.StaminaRestorePercent;
            var staminaRestoreCooldown = reaction.StaminaRestoreCooldown;
            var fpRestoreCooldown = reaction.FPRestoreCooldown;
            var evasionBoost = reaction.EvasionBoost;
            var evasionEnmityBoost = reaction.EvasionEnmityBoost;
            var enmityBoost = reaction.EnmityBoost;
            var defenseBoost = reaction.DefenseBoost;
            var forceDefenseBoost = reaction.ForceDefenseBoost;
            var recastReductionGroup = reaction.RecastReductionGroup;
            var recastReductionSeconds = reaction.RecastReductionSeconds;
            var recastReductionCooldown = reaction.RecastReductionCooldown;
            var nextSkillAbilitySkillType = reaction.NextSkillAbilitySkillType;
            var nextSkillAbilityDamageBonus = reaction.NextSkillAbilityDamageBonus;
            var nextSkillAbilityCriticalRate = reaction.NextSkillAbilityCriticalRate;
            var nextSkillAbilityNoDelay = reaction.NextSkillAbilityNoDelay;
            var nextSkillAbilityDamageWindow = reaction.NextSkillAbilityDamageWindow;
            var nextSkillAbilityCriticalWindow = reaction.NextSkillAbilityCriticalWindow;
            var nextSkillAbilityNoDelayWindow = reaction.NextSkillAbilityNoDelayWindow;
            var nextAutoAttackCriticalRateSkillType = reaction.NextAutoAttackCriticalRateSkillType;
            var nextAutoAttackCriticalRate = reaction.NextAutoAttackCriticalRate;
            var nextAutoAttackCriticalRateWindow = reaction.NextAutoAttackCriticalRateWindow;

            if (staminaRestore > 0 &&
                TryUseStatTrigger(creatureId, staminaRestoreStat, staminaRestoreCooldown))
            {
                using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creatureId, staminaRestoreStat);
                _stat.RestoreStamina(creatureId, staminaRestore);
            }

            if (fpRestore > 0 &&
                TryUseStatTrigger(creatureId, fpRestoreStat, fpRestoreCooldown))
            {
                using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creatureId, fpRestoreStat);
                _stat.RestoreFP(creatureId, fpRestore);
            }

            if (staminaRestorePercent > 0)
            {
                var amount = GameMath.PercentOf(_stat.GetMaxStamina(creatureId), staminaRestorePercent);
                using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creatureId, StatType.DeflectionStaminaRestorePercent);
                _stat.RestoreStamina(creatureId, amount);
            }

            if (evasionBoost != 0 || evasionEnmityBoost != 0)
            {
                ReplaceTemporaryStatModifier(
                    creatureId,
                    StatType.EvasionPercentAdjustment,
                    evasionBoost,
                    DeflectionEvasionBoostDurationSeconds,
                    StatType.DeflectionEvasionPercentAdjustment);
                ReplaceTemporaryStatModifier(
                    creatureId,
                    StatType.EnmityPercentAdjustment,
                    evasionEnmityBoost,
                    DeflectionEvasionBoostDurationSeconds,
                    StatType.DeflectionEvasionPercentAdjustment);
            }

            if (enmityBoost != 0)
            {
                ReplaceTemporaryStatModifier(
                    creatureId,
                    StatType.EnmityPercentAdjustment,
                    enmityBoost,
                    DeflectionEnmityBoostDurationSeconds,
                    StatType.DeflectionEnmityPercentAdjustment);
            }

            if (defenseBoost != 0 || forceDefenseBoost != 0)
            {
                ReplaceTemporaryStatModifier(
                    creatureId,
                    StatType.PhysicalDefensePercentAdjustment,
                    defenseBoost,
                    DeflectionDefenseBoostDurationSeconds,
                    StatType.DeflectionDefensePercentAdjustment);
                ReplaceTemporaryStatModifier(
                    creatureId,
                    StatType.ForceDefensePercentAdjustment,
                    forceDefenseBoost,
                    DeflectionDefenseBoostDurationSeconds,
                    StatType.DeflectionDefensePercentAdjustment);
            }

            // Rate limited like every other deflection payload: the trigger fires once per
            // incoming attack, so without a gate the reduction outpaces the cooldown it shortens
            // as soon as a second attacker joins. The gate is only spent when there is actually a
            // cooldown to shorten, so deflecting while the ability is ready does not burn the
            // window that the next real cooldown needs.
            if (recastReductionGroup != RecastGroup.Invalid && recastReductionSeconds > 0)
            {
                var (isOnRecastDelay, _) = _recast.IsOnRecastDelay(creatureId, recastReductionGroup);
                if (isOnRecastDelay &&
                    TryUseStatTrigger(
                        creatureId,
                        StatType.DeflectionRecastReductionSeconds,
                        recastReductionCooldown))
                {
                    _recast.ReduceRecastDelay(creatureId, recastReductionGroup, recastReductionSeconds);
                }
            }

            GrantNextSkillAbilityBonuses(
                creatureId,
                nextSkillAbilitySkillType,
                nextSkillAbilityDamageBonus,
                nextSkillAbilityCriticalRate,
                Math.Max(nextSkillAbilityDamageWindow, nextSkillAbilityCriticalWindow));

            GrantNextAutoAttackCriticalRateBonus(
                creatureId,
                nextAutoAttackCriticalRateSkillType,
                nextAutoAttackCriticalRate,
                nextAutoAttackCriticalRateWindow);

            if (nextSkillAbilityNoDelay > 0)
            {
                GrantNextAbilityNoDelay(
                    creatureId,
                    nextSkillAbilitySkillType,
                    nextSkillAbilityNoDelayWindow);
            }
        }

        /// <summary>
        /// Reads every payload a deflection of this source grants, in the order the reactions always read them, before any of
        /// them applies.
        /// </summary>
        private void ReadDeflectionReaction(uint creatureId, DeflectionSource source, out DeflectionReaction reaction)
        {
            var staminaRestoreStat = GetDeflectionStatTypeForSource(
                source,
                StatType.MeleeDeflectionStaminaRestore,
                StatType.ShieldDeflectionStaminaRestore);
            var staminaRestoreCooldownStat = GetDeflectionStatTypeForSource(
                source,
                StatType.MeleeDeflectionStaminaRestoreCooldownSeconds,
                StatType.ShieldDeflectionStaminaRestoreCooldownSeconds);
            var staminaRestore = staminaRestoreStat != StatType.Invalid
                ? _stat.GetStatAdjustment(creatureId, staminaRestoreStat)
                : 0;
            var fpRestoreStat = GetDeflectionStatTypeForSource(
                source,
                StatType.MeleeDeflectionFPRestore,
                StatType.DeflectionFPRestore);
            var fpRestore = fpRestoreStat != StatType.Invalid
                ? _stat.GetStatAdjustment(creatureId, fpRestoreStat)
                : 0;
            var fpRestoreCooldownStat = GetDeflectionStatTypeForSource(
                source,
                StatType.MeleeDeflectionFPRestoreCooldownSeconds,
                StatType.DeflectionFPRestoreCooldownSeconds);
            var staminaRestorePercent = _stat.GetStatAdjustment(creatureId, StatType.DeflectionStaminaRestorePercent);
            var staminaRestoreCooldown = staminaRestoreCooldownStat != StatType.Invalid
                ? _stat.GetStatAdjustment(creatureId, staminaRestoreCooldownStat)
                : 0;
            var fpRestoreCooldown = fpRestoreCooldownStat != StatType.Invalid
                ? _stat.GetStatAdjustment(creatureId, fpRestoreCooldownStat)
                : 0;
            var evasionBoost = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionEvasionPercentAdjustment, source);
            var evasionEnmityBoost = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionEvasionEnmityPercentAdjustment, source);
            var enmityBoost = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionEnmityPercentAdjustment, source);
            var defenseBoost = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionDefensePercentAdjustment, source);
            var forceDefenseBoost = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionForceDefensePercentAdjustment, source);
            var recastReductionGroup = GetRecastGroupFromStat(GetDeflectionStatAdjustment(
                creatureId,
                StatType.DeflectionRecastReductionGroupId,
                source));
            var recastReductionSeconds = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionRecastReductionSeconds, source);
            var recastReductionCooldown = GetDeflectionStatAdjustment(
                creatureId,
                StatType.DeflectionRecastReductionCooldownSeconds,
                source);
            var nextSkillAbilitySkillType = GetSkillTypeFromStat(GetDeflectionStatAdjustment(
                creatureId,
                StatType.DeflectionNextSkillAbilitySkillType,
                source));
            var nextSkillAbilityDamageBonus = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionNextSkillAbilityDamageBonus, source);
            var nextSkillAbilityCriticalRate = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionNextSkillAbilityCriticalRatePercentAdjustment, source);
            var nextSkillAbilityNoDelay = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionNextSkillAbilityNoDelay, source);
            var nextSkillAbilityDamageWindow = _stat.GetStatAdjustment(creatureId, StatType.DeflectionNextSkillAbilityDamageBonusWindowSeconds);
            var nextSkillAbilityCriticalWindow = _stat.GetStatAdjustment(creatureId, StatType.DeflectionNextSkillAbilityCriticalRateWindowSeconds);
            var nextSkillAbilityNoDelayWindow = _stat.GetStatAdjustment(creatureId, StatType.DeflectionNextSkillAbilityNoDelayWindowSeconds);
            var nextAutoAttackCriticalRateSkillType = GetSkillTypeFromStat(GetDeflectionStatAdjustment(
                creatureId,
                StatType.DeflectionNextAutoAttackCriticalRateSkillType,
                source));
            var nextAutoAttackCriticalRate = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionNextAutoAttackCriticalRatePercentAdjustment, source);
            var nextAutoAttackCriticalRateWindow = _stat.GetStatAdjustment(creatureId, StatType.DeflectionNextAutoAttackCriticalRateWindowSeconds);

            reaction = new DeflectionReaction
            {
                StaminaRestoreStat = staminaRestoreStat,
                StaminaRestoreCooldownStat = staminaRestoreCooldownStat,
                StaminaRestore = staminaRestore,
                FPRestoreStat = fpRestoreStat,
                FPRestore = fpRestore,
                FPRestoreCooldownStat = fpRestoreCooldownStat,
                StaminaRestorePercent = staminaRestorePercent,
                StaminaRestoreCooldown = staminaRestoreCooldown,
                FPRestoreCooldown = fpRestoreCooldown,
                EvasionBoost = evasionBoost,
                EvasionEnmityBoost = evasionEnmityBoost,
                EnmityBoost = enmityBoost,
                DefenseBoost = defenseBoost,
                ForceDefenseBoost = forceDefenseBoost,
                RecastReductionGroup = recastReductionGroup,
                RecastReductionSeconds = recastReductionSeconds,
                RecastReductionCooldown = recastReductionCooldown,
                NextSkillAbilitySkillType = nextSkillAbilitySkillType,
                NextSkillAbilityDamageBonus = nextSkillAbilityDamageBonus,
                NextSkillAbilityCriticalRate = nextSkillAbilityCriticalRate,
                NextSkillAbilityNoDelay = nextSkillAbilityNoDelay,
                NextSkillAbilityDamageWindow = nextSkillAbilityDamageWindow,
                NextSkillAbilityCriticalWindow = nextSkillAbilityCriticalWindow,
                NextSkillAbilityNoDelayWindow = nextSkillAbilityNoDelayWindow,
                NextAutoAttackCriticalRateSkillType = nextAutoAttackCriticalRateSkillType,
                NextAutoAttackCriticalRate = nextAutoAttackCriticalRate,
                NextAutoAttackCriticalRateWindow = nextAutoAttackCriticalRateWindow,
            };
        }

        private RecastGroup GetRecastGroupFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(RecastGroup), value)
                ? (RecastGroup)value
                : RecastGroup.Invalid;
        }

        private int GetDeflectionStatAdjustment(uint creature, StatType statType, DeflectionSource source)
        {
            return _stat.GetStatTypeDeflectionSource(statType) == source
                ? _stat.GetStatAdjustment(creature, statType)
                : 0;
        }

        private StatType GetDeflectionStatTypeForSource(
            DeflectionSource source,
            StatType first,
            StatType second)
        {
            if (_stat.GetStatTypeDeflectionSource(first) == source)
                return first;

            return _stat.GetStatTypeDeflectionSource(second) == source
                ? second
                : StatType.Invalid;
        }

        private SkillType GetSkillTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(SkillType), value)
                ? (SkillType)value
                : SkillType.Invalid;
        }

        // The native read of the deflection reactions: they need only the creature's id, so a test can run them.
        protected virtual uint GetNativeCreatureId(CNWSCreature creature) => creature.m_idSelf;

        // The calls into the other Combat services and the temporary stat modifiers, one line each, so a test can answer them.

        protected virtual void ReplaceTemporaryStatModifier(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType) =>
            _temporaryStatModifier.Replace(creature, statType, amount, durationSeconds, groupStatType);

        protected virtual void TrackDeflection(uint creature, DeflectionSource source) => _damageTakenEffects.TrackDeflection(creature, source);

        protected virtual bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds) =>
            _resources.TryUseStatTrigger(creature, statType, cooldownSeconds);

        protected virtual void GrantNextSkillAbilityBonuses(uint creature, SkillType skillType, int damageBonus, int criticalRate, int durationSeconds) =>
            _attackTiming.GrantNextSkillAbilityBonuses(creature, skillType, damageBonus, criticalRate, durationSeconds);

        protected virtual void GrantNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType, int criticalRate, int durationSeconds) =>
            _attackTiming.GrantNextAutoAttackCriticalRateBonus(creature, skillType, criticalRate, durationSeconds);

        protected virtual void GrantNextAbilityNoDelay(uint creature, SkillType skillType, int durationSeconds) =>
            _attackTiming.GrantNextAbilityNoDelay(creature, skillType, durationSeconds);
    }
}
