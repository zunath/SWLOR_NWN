using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class DefeatedEnemyEffects
    {
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void ApplyDefeatedEnemyEffects()
        {
            var defeated = OBJECT_SELF;
            var killer = GetLastKiller();

            if (GetIsObjectValid(killer) && killer != defeated)
            {
                DefeatedEnemyEffects.ApplyDefeatedEnemyEffects(killer, defeated);
            }

            DefeatedEnemyEffects.ApplyRecentDamagerDefeatedEnemyEffects(defeated);
            CombatStatTriggers.RemoveStatTriggerCooldowns(defeated);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearStatTriggerState()
        {
            var creature = GetExitingObject();
            if (!GetIsObjectValid(creature))
                return;

            CombatStatTriggers.RemoveStatTriggerCooldowns(creature);
        }

        public static void ApplyDefeatedEnemyEffects(uint creature, uint defeated = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
                return;

            if (GetIsObjectValid(defeated))
            {
                DefeatedEnemyEffects.ApplyPoisonedDefeatedEnemySpread(defeated, creature);
                DefeatedEnemyEffects.ApplyDefeatedBleedingEnemySpread(creature, defeated);
            }

            var staminaRestore = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyStaminaRestore);
            if (staminaRestore > 0)
            {
                Stat.RestoreStamina(creature, staminaRestore);
            }

            var fpRestore = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyFPRestore);
            if (fpRestore > 0)
            {
                Stat.RestoreFP(creature, fpRestore);
            }

            var hpRestorePercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                DamageModifierPipeline.HealPercentOfMaxHP(creature, hpRestorePercent);
            }

            var attackPercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackPercentAdjustment);
            var attackDuration = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDurationSeconds);
            if (attackPercent != 0 && attackDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.AttackPercentAdjustment,
                    attackPercent,
                    attackDuration,
                    StatType.DefeatedEnemyAttackPercentAdjustment);
            }

            var hastePercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDelayReductionPercent);
            var hasteDuration = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDelayReductionDurationSeconds);
            if (hastePercent != 0 && hasteDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.AttackDelayReductionPercent,
                    hastePercent,
                    hasteDuration,
                    StatType.DefeatedEnemyAttackDelayReductionPercent);
            }

            var allyDefensePercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment);
            var allyDefenseDuration = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds);
            if (allyDefensePercent != 0 && allyDefenseDuration > 0)
            {
                DefeatedEnemyEffects.ApplyDefeatedEnemyNearbyAllyDefense(creature, allyDefensePercent, allyDefenseDuration);
            }

            DefeatedEnemyEffects.ApplyHitPointSpendDefeatedEnemyEffects(creature);
        }

        internal static void ApplyHitPointSpendDefeatedEnemyEffects(uint creature)
        {
            if (Stat.GetStatAdjustment(creature, StatType.HeavyVibrobladeOffenseSoulAscension) <= 0)
                return;

            var marker = TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.HeavyVibrobladeOffenseSoulAscension,
                StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds);
            if (marker <= 0)
                return;

            StatusEffect.ApplyStatusEffect(creature, creature, typeof(SoulAscensionStatusEffect), 30f);
        }

        internal static void ApplyRecentDamagerDefeatedEnemyEffects(uint defeated)
        {
            const float RecentDamageWindowSeconds = 6f;

            if (!GetIsObjectValid(defeated))
                return;

            foreach (var source in CombatState.GetRecentDamageSourcesForTarget(defeated, RecentDamageWindowSeconds))
            {
                DefeatedEnemyEffects.ApplyCruelMomentumEffect(source);
            }
        }

        internal static void ApplyCruelMomentumEffect(uint creature)
        {
            if (!GetIsObjectValid(creature) ||
                GetIsDead(creature) ||
                Stat.GetStatAdjustment(creature, StatType.CruelMomentum) <= 0 ||
                !CombatStatTriggers.TryUseStatTrigger(creature, StatType.CruelMomentum, 10))
            {
                return;
            }

            Stat.RestoreFP(creature, 2);
            StatusEffect.ApplyStatusEffect(creature, creature, typeof(CruelMomentumStatusEffect), 30f);
        }

        internal static void ApplyDefeatedEnemyNearbyAllyDefense(
            uint creature,
            int physicalDefensePercent,
            int durationSeconds)
        {
            const float Range = 5f;

            foreach (var member in Party.GetAllPartyMembersWithinRange(creature, Range))
            {
                if (member == creature)
                    continue;

                TemporaryStatModifier.Replace(
                    member,
                    StatType.PhysicalDefensePercentAdjustment,
                    physicalDefensePercent,
                    durationSeconds,
                StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment);
            }
        }

        internal static void ApplyPoisonedDefeatedEnemySpread(uint defeated, uint fallbackSource)
        {
            var poisonEffects = StatusEffect.GetCreatureStatusEffects(defeated)
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
                var radius = Stat.GetStatAdjustment(source, StatType.PoisonedDefeatedEnemySpreadRadiusMeters);
                var duration = Stat.GetStatAdjustment(source, StatType.PoisonedDefeatedEnemySpreadDurationSeconds);
                if (radius <= 0 || duration <= 0)
                    continue;

                var target = DefeatedEnemyEffects.GetNearestHostileCreatureWithinRange(source, defeated, radius, defeated);
                if (!GetIsObjectValid(target))
                    continue;

                StatusEffect.ApplyStatusEffect(source, target, typeof(PoisonStatusEffect), duration, CombatDamageType.Physical);
                return;
            }
        }

        internal static void ApplyDefeatedBleedingEnemySpread(uint creature, uint defeated)
        {
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(defeated) ||
                !StatusEffect.HasStatusEffectCategory(defeated, StatusEffectCategory.Bleeding))
            {
                return;
            }

            var duration = Stat.GetStatAdjustment(creature, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds);
            if (duration <= 0)
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(
                         creature,
                         GetLocation(defeated),
                         5f,
                         3,
                         defeated))
            {
                StatusEffect.ApplyStatusEffect(creature, nearby, typeof(BleedStatusEffect), duration, CombatDamageType.Physical);
            }
        }

        internal static uint GetNearestHostileCreatureWithinRange(
            uint source,
            uint origin,
            float radius,
            uint excludedTarget = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(source) || !GetIsObjectValid(origin) || radius <= 0f)
                return OBJECT_INVALID;

            var originLocation = GetLocation(origin);
            var nearest = OBJECT_INVALID;
            var nearestDistance = float.MaxValue;
            var creature = GetFirstObjectInShape(Shape.Sphere, radius, originLocation, true);
            while (GetIsObjectValid(creature))
            {
                if (creature != excludedTarget &&
                    GetIsReactionTypeHostile(creature, source) &&
                    !GetIsDead(creature))
                {
                    var distance = GetDistanceBetween(origin, creature);
                    if (distance < nearestDistance)
                    {
                        nearest = creature;
                        nearestDistance = distance;
                    }
                }

                creature = GetNextObjectInShape(Shape.Sphere, radius, originLocation, true);
            }

            return nearest;
        }

    }
}
