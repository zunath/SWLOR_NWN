using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class GuardDeflection
    {
        private const int BaseGuardDamageReductionPercent = 20;
        private const int MaximumGuardDamageReductionPercent = 40;

        public static void TrackGuardedHit(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            CombatActivity.TrackGuardedHit(creature);
            GuardDeflection.ApplyGuardedHitNextSkillAbilityEffects(creature);
            GuardDeflection.ApplyGuardedHitNextSkillAbilityStatusEffects(creature);
        }

        public static void TrackAvoidedAttack(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            var skillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(creature, StatType.AvoidedAttackNextSkillAbilitySkillType));
            var adjustment = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment);
            var damageBonus = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackNextSkillAbilityDamageBonus);
            var window = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackNextSkillAbilityWindowSeconds);
            AbilityImpactEffects.GrantNextSkillAbilityStaminaCostAdjustment(creature, skillType, adjustment, window);
            AbilityImpactEffects.GrantNextSkillAbilityBonuses(creature, skillType, damageBonus, 0, window);

            var chance = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackStaminaRestoreChance);
            var staminaRestore = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackStaminaRestore);
            var staminaRestoreCooldown = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackStaminaRestoreCooldownSeconds);
            if (chance > 0 &&
                staminaRestore > 0 &&
                Random.D100(1) <= chance &&
                CombatStatTriggers.TryUseStatTrigger(creature, StatType.AvoidedAttackStaminaRestore, staminaRestoreCooldown))
            {
                Stat.RestoreStamina(creature, staminaRestore);
            }

            var singleChance = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackSingleStaminaRestoreChance);
            var singleStaminaRestore = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackSingleStaminaRestore);
            if (singleChance > 0 && singleStaminaRestore > 0 && Random.D100(1) <= singleChance)
            {
                Stat.RestoreStamina(creature, singleStaminaRestore);
                StatusEffect.RemoveStatusEffectsWithStat(creature, StatType.AvoidedAttackSingleStaminaRestore, false);
            }

            GuardDeflection.ApplyAvoidedAttackAbilityUsedEvasionRefresh(creature);
            GuardDeflection.ApplyAvoidedAttackNextAutoAttackNoDelay(creature);
            GuardDeflection.ApplyAvoidedAttackAccuracy(creature);
        }

        internal static void ApplyAvoidedAttackAccuracy(uint creature)
        {
            var accuracy = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackAccuracyPercentAdjustment);
            var duration = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackAccuracyDurationSeconds);
            if (accuracy == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.AccuracyPercentAdjustment,
                accuracy,
                duration,
                StatType.AvoidedAttackAccuracyPercentAdjustment);
        }

        internal static void ApplyAvoidedAttackAbilityUsedEvasionRefresh(uint creature)
        {
            var duration = Stat.GetStatAdjustment(
                creature,
                StatType.AvoidedAttackAbilityUsedEvasionRefreshDurationSeconds);
            if (duration <= 0)
                return;

            var evasionPercent = Stat.GetStatAdjustment(
                creature,
                StatType.AbilityUsedEvasionPercentAdjustment);
            if (evasionPercent <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.EvasionPercentAdjustment,
                evasionPercent,
                duration,
                StatType.EvasionPercentAdjustment);
        }

        internal static void ApplyAvoidedAttackNextAutoAttackNoDelay(uint creature)
        {
            var skillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AvoidedAttackNextAutoAttackNoDelaySkillType));
            var duration = Stat.GetStatAdjustment(
                creature,
                StatType.AvoidedAttackNextAutoAttackNoDelayDurationSeconds);

            QueuedCombatActions.GrantNextAutoAttackNoDelay(creature, skillType, duration);
        }

        public static void ApplyMeleeDamageTakenEffects(uint defender, uint attacker)
        {
            if (!GetIsObjectValid(defender) || !GetIsObjectValid(attacker))
                return;

            GuardDeflection.ApplyMeleeDamageTakenPoisonDamage(defender, attacker);

            var chance = Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenStaminaRestoreChance);
            if (chance <= 0 || Random.D100(1) > chance)
                return;

            var staminaRestore = Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenStaminaRestore);
            if (staminaRestore > 0)
            {
                Stat.RestoreStamina(defender, staminaRestore);
            }

            var evasion = Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenEvasionPercentAdjustment);
            var duration = Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenEvasionDurationSeconds);
            if (evasion != 0 && duration > 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.EvasionPercentAdjustment,
                    evasion,
                    duration,
                    StatType.MeleeDamageTakenEvasionPercentAdjustment);
            }
        }

        internal static void ApplyMeleeDamageTakenPoisonDamage(uint defender, uint attacker)
        {
            var chance = Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenPoisonDamageChance);
            var damage = Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenPoisonDamage);
            if (chance <= 0 || damage <= 0 || Random.D100(1) > chance)
                return;

            var scalingAbility = AbilityImpactEffects.GetAbilityTypeFromStatPlusOne(
                Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenPoisonDamageScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                damage = AbilityEffectScaling.ScaleDirectEffect(
                    damage,
                    GetAbilityScore(defender, scalingAbility),
                    source: defender);
            }

            var appliedDamage = TriggeredCombatEffects.ApplyTriggeredDamage(defender, attacker, damage, CombatDamageType.Poison);
            if (appliedDamage <= 0)
                return;

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Poison_S), attacker);
        }

        public static int ApplyGuardedHitModifiers(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            if (!GetIsObjectValid(defender) ||
                !GetIsObjectValid(attacker) ||
                defender == attacker ||
                damage <= 0 ||
                !damageType.IsPhysicalDamageType())
                return damage;

            var guardChance = Stat.GetGuardChance(defender);
            if (guardChance <= 0 || Random.D100(1) > guardChance)
                return damage;

            var reductionPercent = GuardDeflection.GetGuardDamageReductionPercent(defender);
            var preventedDamage = Math.Min(damage, Math.Max(1, (int)Math.Ceiling(damage * (reductionPercent / 100f))));
            var adjustedDamage = Math.Max(0, damage - preventedDamage);

            TrackGuardedHit(defender);
            StatusEffect.OnGuardedHit(defender, attacker, preventedDamage);
            GuardDeflection.ApplyGuardedHitRecovery(defender);
            GuardDeflection.ApplyGuardedHitRetaliation(attacker, defender);
            GuardDeflection.ApplyGuardedHitEnmity(attacker, defender, damage);
            GuardDeflection.SendGuardedHitFeedback(defender, attacker, preventedDamage);

            return adjustedDamage;
        }

        internal static void SendGuardedHitFeedback(uint defender, uint attacker, int preventedDamage)
        {
            if (GetIsPC(defender))
            {
                var feedback = GuardDeflection.BuildGuardedHitFeedback(defender, defender, attacker, preventedDamage);
                SendMessageToPC(defender, feedback);
                FloatingTextStringOnCreature(ColorToken.Combat($"Guard (-{preventedDamage})"), defender, false);
            }

            if (GetIsPC(attacker))
            {
                var feedback = GuardDeflection.BuildGuardedHitFeedback(attacker, defender, attacker, preventedDamage);
                SendMessageToPC(attacker, feedback);
            }
        }

        internal static string BuildGuardedHitFeedback(uint observer, uint defender, uint attacker, int preventedDamage)
        {
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);
            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            return ColorToken.Combat($"{defenderName} guards against {attackerName}'s attack, preventing {preventedDamage} damage.");
        }

        public static void SendIncomingCriticalHitDowngradeFeedback(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(defender))
                return;

            if (GetIsPC(defender))
            {
                var feedback = GuardDeflection.BuildIncomingCriticalHitDowngradeCombatLogMessage(defender, attacker, defender);
                SendMessageToPC(defender, feedback);
                FloatingTextStringOnCreature(ColorToken.Combat("Critical Ward"), defender, false);
            }

            if (GetIsObjectValid(attacker) &&
                attacker != defender &&
                GetIsPC(attacker))
            {
                var feedback = GuardDeflection.BuildIncomingCriticalHitDowngradeCombatLogMessage(attacker, attacker, defender);
                SendMessageToPC(attacker, feedback);
            }
        }

        internal static string BuildIncomingCriticalHitDowngradeCombatLogMessage(uint observer, uint attacker, uint defender)
        {
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);

            if (!GetIsObjectValid(attacker) || attacker == defender)
                return ColorToken.Combat($"{defenderName}'s Critical Ward negates the critical hit.");

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            return ColorToken.Combat($"{defenderName}'s Critical Ward negates {attackerName}'s critical hit.");
        }

        internal static int GetGuardDamageReductionPercent(uint defender)
        {
            var adjustment = Stat.GetStatAdjustment(defender, StatType.GuardDamageReductionPercentAdjustment);
            return Math.Clamp(
                BaseGuardDamageReductionPercent + adjustment,
                0,
                MaximumGuardDamageReductionPercent);
        }

        internal static void ApplyGuardedHitRecovery(uint defender)
        {
            var staminaRestore = Stat.GetStatAdjustment(defender, StatType.GuardStaminaRestore);
            if (staminaRestore > 0)
            {
                Stat.RestoreStamina(defender, staminaRestore);
            }
        }

        internal static void ApplyGuardedHitRetaliation(uint attacker, uint defender)
        {
            var skillType = QueuedCombatActions.GetEquippedWeaponSkillType(defender);
            var retaliationDamage = Stat.GetStatAdjustment(defender, StatType.GuardRetaliationDamage);
            var bonusSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                defender,
                StatType.GuardRetaliationDamageBonusSkillType));
            if (AbilityImpactEffects.SkillTypeMatches(skillType, bonusSkillType))
            {
                retaliationDamage += Stat.GetStatAdjustment(defender, StatType.GuardRetaliationDamageBonus);
            }

            if (retaliationDamage <= 0)
                return;

            var scalingAbility = GuardDeflection.GetGuardRetaliationDamageAbility(defender, skillType);
            retaliationDamage = AbilityEffectScaling.ScaleDirectEffect(
                retaliationDamage,
                GetAbilityScore(defender, scalingAbility),
                source: defender);

            TriggeredCombatEffects.ApplyTriggeredDamage(defender, attacker, retaliationDamage, CombatDamageType.Physical, skillType);
        }

        internal static AbilityType GetGuardRetaliationDamageAbility(uint defender, SkillType skillType)
        {
            var weapon = AbilityHitResolver.GetRelevantSkillWeapon(defender, skillType);
            if (!GetIsObjectValid(weapon))
                return AbilityType.Might;

            var ability = CombatWeaponStats.GetWeaponDamageAbilityType(defender, GetBaseItemType(weapon));
            return ability == AbilityType.Invalid
                ? AbilityType.Might
                : ability;
        }

        internal static void ApplyGuardedHitEnmity(uint attacker, uint defender, int damage)
        {
            var enmity = Math.Max(1, damage);
            var percentAdjustment = Stat.GetStatAdjustment(defender, StatType.GuardEnmityPercentAdjustment);
            if (percentAdjustment != 0)
            {
                enmity = Math.Max(1, (int)Math.Ceiling(enmity * ((100 + percentAdjustment) / 100f)));
            }

            Enmity.ModifyEnmity(defender, attacker, enmity);
        }

        public static void TrackDeflection(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            CombatActivity.TrackDeflection(creature);
            GuardDeflection.ApplyDeflectionNearbyAllyGuard(creature);
        }

        internal static void ApplyDeflectionNearbyAllyGuard(uint creature)
        {
            var guard = Stat.GetStatAdjustment(creature, StatType.DeflectionNearbyAllyGuard);
            var duration = Stat.GetStatAdjustment(creature, StatType.DeflectionNearbyAllyGuardDurationSeconds);
            if (guard <= 0 || duration <= 0)
                return;

            var ally = AbilityTargeting.GetFriendlyTargetsNearLocation(creature, GetLocation(creature), 5f, false)
                .Where(target => target != creature)
                .OrderBy(target => GetDistanceBetween(creature, target))
                .FirstOrDefault();
            if (!GetIsObjectValid(ally))
                return;

            TemporaryStatModifier.Replace(
                ally,
                StatType.Guard,
                guard,
                duration,
                StatType.DeflectionNearbyAllyGuard);
        }

        internal static void ApplyGuardedHitNextSkillAbilityEffects(uint creature)
        {
            var skillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilitySkillType));
            var criticalRate = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityCriticalRatePercentAdjustment);
            var damageBonus = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityDamageBonus);
            var window = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityWindowSeconds);

            AbilityImpactEffects.GrantNextSkillAbilityBonuses(creature, skillType, damageBonus, criticalRate, window);
        }

        internal static void ApplyGuardedHitNextSkillAbilityStatusEffects(uint creature)
        {
            var skillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextSkillAbilityStatusSkillType));
            var duration = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            var damageBonus = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityExposedDamageBonus);
            if (skillType == SkillType.Invalid || duration <= 0 && damageBonus <= 0)
                return;

            var window = Math.Max(1, duration);
            TemporaryStatModifier.Replace(
                creature,
                StatType.GuardedHitNextSkillAbilityStatusSkillType,
                (int)skillType,
                window,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);

            if (duration > 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.GuardedHitNextSkillAbilityExposedDurationSeconds,
                    duration,
                    window,
                    StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            }

            if (damageBonus > 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.GuardedHitNextSkillAbilityExposedDamageBonus,
                    damageBonus,
                    window,
                    StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            }
        }

        internal static int ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(uint creature, SkillType skillType)
        {
            var storedSkillType = AbilityImpactEffects.GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextSkillAbilityStatusSkillType,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, storedSkillType))
                return 0;

            return TemporaryStatModifier.Consume(
                creature,
                StatType.GuardedHitNextSkillAbilityExposedDamageBonus,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
        }

    }
}
