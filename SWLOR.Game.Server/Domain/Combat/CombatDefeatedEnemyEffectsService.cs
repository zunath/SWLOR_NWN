using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What a kill sets off for the killer: the poison and bleeding spreads, the stamina, FP and HP restores, the attack and haste
    /// grants, the nearby allies' defense, the hit point spend reward and, for the recent damagers of the defeated enemy, cruel momentum.
    /// </summary>
    public class CombatDefeatedEnemyEffectsService : ICombatDefeatedEnemyEffectsService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatActivityService _combatActivity;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IPartyRoster _party;

        public CombatDefeatedEnemyEffectsService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatActivityService combatActivity,
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier,
            ICombatRecorderService combatRecorder,
            IPartyRoster party)
        {
            _queries = queries;
            _resources = resources;
            _combatActivity = combatActivity;
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
            _combatRecorder = combatRecorder;
            _party = party;
        }

        public void ApplyDefeatedEnemyEffects(uint creature, uint defeated = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
                return;

            using var recorderAttribution = _combatRecorder.BeginAttribution(creature, CombatRecordSourceKind.OnKill);

            if (GetIsObjectValid(defeated))
            {
                ApplyPoisonedDefeatedEnemySpread(defeated, creature);
                ApplyDefeatedBleedingEnemySpread(creature, defeated);
            }

            var staminaRestore = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyStaminaRestore);
            if (staminaRestore > 0)
            {
                _stat.RestoreStamina(creature, staminaRestore);
            }

            var fpRestore = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyFPRestore);
            if (fpRestore > 0)
            {
                _stat.RestoreFP(creature, fpRestore);
            }

            var hpRestorePercent = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                _resources.HealFromMaxHP(creature, hpRestorePercent);
            }

            var attackPercent = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackPercentAdjustment);
            var attackDuration = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDurationSeconds);
            if (attackPercent != 0 && attackDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    creature,
                    StatType.AttackPercentAdjustment,
                    attackPercent,
                    attackDuration,
                    StatType.DefeatedEnemyAttackPercentAdjustment);
            }

            var hastePercent = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDelayReductionPercent);
            var hasteDuration = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDelayReductionDurationSeconds);
            if (hastePercent != 0 && hasteDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    creature,
                    StatType.AttackDelayReductionPercent,
                    hastePercent,
                    hasteDuration,
                    StatType.DefeatedEnemyAttackDelayReductionPercent);
            }

            var allyDefensePercent = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment);
            var allyDefenseDuration = _stat.GetStatAdjustment(creature, StatType.DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds);
            if (allyDefensePercent != 0 && allyDefenseDuration > 0)
            {
                ApplyDefeatedEnemyNearbyAllyDefense(creature, allyDefensePercent, allyDefenseDuration);
            }

            ApplyHitPointSpendDefeatedEnemyEffects(creature);
        }

        private void ApplyHitPointSpendDefeatedEnemyEffects(uint creature)
        {
            if (_stat.GetStatAdjustment(creature, StatType.HeavyVibrobladeOffenseSoulAscension) <= 0)
                return;

            var marker = _temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.HeavyVibrobladeOffenseSoulAscension,
                StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds);
            if (marker <= 0)
                return;

            _statusEffectApplication.ApplyStatusEffect(creature, creature, typeof(SoulAscensionStatusEffect), 30f);
        }

        public void ApplyRecentDamagerDefeatedEnemyEffects(uint defeated)
        {
            const float RecentDamageWindowSeconds = 6f;

            if (!GetIsObjectValid(defeated))
                return;

            var now = Clock.UtcNow;
            var recentDamagers = _combatActivity.RecentDamageTargets
                .Where(x => x.Key.Item2 == defeated)
                .ToList();

            foreach (var ((source, target), lastDamaged) in recentDamagers)
            {
                if ((now - lastDamaged).TotalSeconds > RecentDamageWindowSeconds)
                {
                    _combatActivity.RecentDamageTargets.Remove((source, target));
                    continue;
                }

                ApplyCruelMomentumEffect(source);
            }
        }

        private void ApplyCruelMomentumEffect(uint creature)
        {
            if (!GetIsObjectValid(creature) ||
                GetIsDead(creature) ||
                _stat.GetStatAdjustment(creature, StatType.CruelMomentum) <= 0 ||
                !_resources.TryUseStatTrigger(creature, StatType.CruelMomentum, 10))
            {
                return;
            }

            _resources.RestoreFPFromStat(creature, StatType.CruelMomentum, 2);
            _statusEffectApplication.ApplyStatusEffect(creature, creature, typeof(CruelMomentumStatusEffect), 30f);
        }

        private void ApplyDefeatedEnemyNearbyAllyDefense(
            uint creature,
            int physicalDefensePercent,
            int durationSeconds)
        {
            const float Range = 5f;

            foreach (var member in _party.GetAllPartyMembersWithinRange(creature, Range))
            {
                if (member == creature)
                    continue;

                _temporaryStatModifier.Replace(
                    member,
                    StatType.PhysicalDefensePercentAdjustment,
                    physicalDefensePercent,
                    durationSeconds,
                StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment);
            }
        }

        private void ApplyPoisonedDefeatedEnemySpread(uint defeated, uint fallbackSource)
        {
            var poisonEffects = _statusEffect.GetCreatureStatusEffects(defeated)
                .GetAllEffects()
                .Where(effect => effect is PoisonStatusEffect)
                .ToList();
            if (poisonEffects.Count <= 0)
                return;

            foreach (var poisonEffect in poisonEffects)
            {
                var source = GetIsObjectValid(poisonEffect.Source)
                    ? poisonEffect.Source
                    : fallbackSource;
                var radius = _stat.GetStatAdjustment(source, StatType.PoisonedDefeatedEnemySpreadRadiusMeters);
                var duration = _stat.GetStatAdjustment(source, StatType.PoisonedDefeatedEnemySpreadDurationSeconds);
                if (radius <= 0 || duration <= 0)
                    continue;

                var target = _queries.GetNearestHostileCreatureWithinRange(source, defeated, radius, defeated);
                if (!GetIsObjectValid(target))
                    continue;

                _statusEffectApplication.ApplyStatusEffect(source, target, typeof(PoisonStatusEffect), duration, CombatDamageType.Physical);
                return;
            }
        }

        private void ApplyDefeatedBleedingEnemySpread(uint creature, uint defeated)
        {
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(defeated) ||
                !_statusEffect.HasStatusEffectCategory(defeated, StatusEffectCategory.Bleeding))
            {
                return;
            }

            var duration = _stat.GetStatAdjustment(creature, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds);
            if (duration <= 0)
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(
                         creature,
                         GetLocation(defeated),
                         5f,
                         3,
                         defeated))
            {
                _statusEffectApplication.ApplyStatusEffect(creature, nearby, typeof(BleedStatusEffect), duration, CombatDamageType.Physical);
            }
        }

        public void SpreadPoisonFromTarget(uint activator, uint target)
        {
            var radius = _stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentPoisonSpreadRadiusMeters);
            var duration = _stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentPoisonSpreadDurationSeconds);
            if (radius <= 0 || duration <= 0)
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), radius, 0, target))
            {
                _statusEffectApplication.ApplyStatusEffect(activator, nearby, typeof(PoisonStatusEffect), duration, CombatDamageType.Poison);
            }
        }
    }
}
