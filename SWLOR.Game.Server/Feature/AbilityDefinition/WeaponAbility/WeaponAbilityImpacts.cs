using SWLOR.Game.Server.Core;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility
{
    /// <summary>
    /// The hostile impact of a weapon ability: the telegraphed area path and the direct multi-hit path.
    /// </summary>
    public class WeaponAbilityImpacts
    {
        private IAbilityService Ability { get; }
        private IAbilityDamageInputService AbilityDamageInput { get; }
        private IAbilityStatusApplicationService AbilityStatusApplication { get; }
        private ICombatAbilityHitService CombatAbilityHit { get; }
        private WeaponAbilityDamageModifiers WeaponAbilityDamageModifiers { get; }
        private WeaponAbilityHitRiders WeaponAbilityHitRiders { get; }
        private WeaponAbilityImpactEffects WeaponAbilityImpactEffects { get; }
        private WeaponAbilitySelfEffects WeaponAbilitySelfEffects { get; }
        private IAbilityImpactState IAbilityImpactState { get; }

        public WeaponAbilityImpacts(IAbilityService ability, IAbilityDamageInputService abilityDamageInput, IAbilityStatusApplicationService abilityStatusApplication, ICombatAbilityHitService combatAbilityHit, WeaponAbilityDamageModifiers weaponAbilityDamageModifiers, WeaponAbilityHitRiders weaponAbilityHitRiders, WeaponAbilityImpactEffects weaponAbilityImpactEffects, WeaponAbilitySelfEffects weaponAbilitySelfEffects, IAbilityImpactState abilityImpactState)
        {
            Ability = ability;
            AbilityDamageInput = abilityDamageInput;
            AbilityStatusApplication = abilityStatusApplication;
            CombatAbilityHit = combatAbilityHit;
            WeaponAbilityDamageModifiers = weaponAbilityDamageModifiers;
            WeaponAbilityHitRiders = weaponAbilityHitRiders;
            WeaponAbilityImpactEffects = weaponAbilityImpactEffects;
            WeaponAbilitySelfEffects = weaponAbilitySelfEffects;
            IAbilityImpactState = abilityImpactState;
        }

        public void ApplyHostileImpact(WeaponAbilityImpactRequest request, uint activator, uint target, Location targetLocation)
        {
            var profile = request.Profile;
            var skill = request.Skill;
            var baseDamage = request.BaseDamage;
            var duration = request.Duration;
            var statusEffect = request.StatusEffect;
            var additionalStatusEffects = request.AdditionalStatusEffects;
            var isArea = request.IsArea;
            var targetingShape = request.TargetingShape;
            var targetingSizeX = request.TargetingSizeX;
            var targetingSizeY = request.TargetingSizeY;
            var targetingFlags = request.TargetingFlags;
            var areaVisualEffect = request.AreaVisualEffect;
            var combatImpactDamageAbility = request.CombatImpactDamageAbility;
            var impactAnimation = request.ImpactAnimation;
            WeaponAbilitySelfEffects.SpendHitPoints(profile, activator);
            WeaponAbilitySelfEffects.AfterHostileActivation(profile, activator);
            var (impactStatusEffect, impactDuration) = WeaponAbilityDamageModifiers.ResolveImpactStatus(profile, activator, statusEffect, duration);
            var spreadSnapshot = new WeaponAbilityProfile.StatusSpreadSnapshot(profile.MaximumStatusSpreadsPerCast);
            Action<uint> captureSpreadPrerequisites = profile.SpreadBleedFromTarget || profile.SpreadHemorrhageFromTarget || profile.SpreadSunderFromTarget
                ? impactedTarget => spreadSnapshot.Capture(impactedTarget)
                : null;
            var activationIdleBonusSnapshot = WeaponAbilityDamageModifiers.CaptureActivationIdleBonusSnapshot(activator);
            IAbilityImpactState.AddActiveAbilityDefenseIgnorePercentAdjustment(
                activator,
                WeaponAbilityDamageModifiers.GetDefenseIgnorePercent(profile, activator, activationIdleBonusSnapshot));

            if (isArea && ShouldUseTelegraphedCombatImpact(targetingShape, profile))
            {
                var areaDamage = 0;
                try
                {
                    areaDamage = Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        skill,
                        baseDamage,
                        impactDuration,
                        impactStatusEffect,
                        ToCombatImpactAreaShape(targetingShape),
                        profile.TelegraphDuration,
                        targetingSizeX > 0f ? targetingSizeX : 5.0f,
                        targetingSizeY,
                        additionalStatusEffects,
                        CenterAreaOnActivator(targetingFlags),
                        profile.StatusEffectFactory,
                        damageType: profile.DamageType,
                        areaVisualEffect: areaVisualEffect,
                        combatImpactDamageAbility: combatImpactDamageAbility,
                        baseDamageAdjustment: impactedTarget => WeaponAbilityDamageModifiers.GetBaseDamageAdjustment(profile,
                            activator,
                            impactedTarget,
                            activationIdleBonusSnapshot),
                        damagePercentAdjustment: impactedTarget => WeaponAbilityDamageModifiers.GetDamagePercentAdjustment(profile, activator, impactedTarget),
                        afterImpactAction: summary =>
                        {
                            WeaponAbilityImpactEffects.RestoreResourcesForTargetCount(profile, activator, summary);
                            // Instant impacts apply these modifiers in AfterImpact below.
                            if (profile.TelegraphDuration > 0f && summary.ImpactedTargetCount > 0)
                                WeaponAbilitySelfEffects.AfterActivation(profile, activator, skill);
                        },
                        maxTargets: profile.MaximumAreaTargets,
                        enmityBonus: profile.EnmityBonus,
                        beforeImpact: captureSpreadPrerequisites,
                        afterSuccessfulHit: impactedTarget =>
                        {
                            WeaponAbilityHitRiders.AfterSuccessfulHit(profile, activator, impactedTarget, AbilityDamageInput.GetWeaponAbilityDamageType(activator, skill, profile.DamageType), spreadSnapshot);
                            CombatAbilityHit.ApplyRangedAbilityTargetDefenseReduction(activator, impactedTarget, skill);
                        },
                        beforeSuccessfulImpactRiders: impactedTarget => WeaponAbilityHitRiders.BeforeSuccessfulImpactRiders(profile,
                            activator,
                            impactedTarget,
                            AbilityDamageInput.GetWeaponAbilityDamageType(activator, skill, profile.DamageType)),
                        hitChancePercentAdjustment: profile.HitChancePercentAdjustment,
                        criticalRatePercentAdjustment: WeaponAbilityDamageModifiers.GetCriticalRateAdjustment(profile,
                            activator,
                            target,
                            activationIdleBonusSnapshot),
                        impactAnimation: impactAnimation);
                }
                finally
                {
                    WeaponAbilityDamageModifiers.ClearActivationIdleBonusSnapshots(activator);
                }

                WeaponAbilityImpactEffects.AfterImpact(profile,
                    activator,
                    areaDamage,
                    areaDamage > 0 ? 1 : 0,
                    IAbilityImpactState.GetActiveAbilityImpactSummary(activator),
                    clearActivationIdleBonusSnapshots: false);
                return;
            }

            var totalDamage = 0;
            var successfulHitCount = 0;
            var hitCount = Math.Max(1, profile.HitCount);
            for (var hit = 0; hit < hitCount; hit++)
            {
                var hitDamage = Ability.ApplyCombatImpact(
                    activator,
                    target,
                    targetLocation,
                    skill,
                    baseDamage,
                    impactDuration,
                    impactStatusEffect,
                    isArea,
                    additionalStatusEffects,
                    profile.StatusEffectFactory,
                    damageType: profile.DamageType,
                    combatImpactDamageAbility: combatImpactDamageAbility,
                    baseDamageAdjustment: impactedTarget => WeaponAbilityDamageModifiers.GetBaseDamageAdjustment(profile,
                        activator,
                        impactedTarget,
                        activationIdleBonusSnapshot),
                    damagePercentAdjustment: impactedTarget => WeaponAbilityDamageModifiers.GetDamagePercentAdjustment(profile, activator, impactedTarget),
                    enmityBonus: profile.EnmityBonus,
                    beforeImpact: captureSpreadPrerequisites,
                    afterSuccessfulHit: impactedTarget =>
                    {
                        WeaponAbilityHitRiders.AfterSuccessfulHit(profile, activator, impactedTarget, AbilityDamageInput.GetWeaponAbilityDamageType(activator, skill, profile.DamageType), spreadSnapshot);
                        CombatAbilityHit.ApplyRangedAbilityTargetDefenseReduction(activator, impactedTarget, skill);
                    },
                    beforeSuccessfulImpactRiders: impactedTarget => WeaponAbilityHitRiders.BeforeSuccessfulImpactRiders(profile,
                        activator,
                        impactedTarget,
                        AbilityDamageInput.GetWeaponAbilityDamageType(activator, skill, profile.DamageType)),
                    hitChancePercentAdjustment: profile.HitChancePercentAdjustment,
                    criticalRatePercentAdjustment: WeaponAbilityDamageModifiers.GetCriticalRateAdjustment(profile,
                        activator,
                        target,
                        activationIdleBonusSnapshot),
                    playImpactAnimation: hit == 0);
                totalDamage += hitDamage;
                if (hitDamage > 0)
                    successfulHitCount++;
            }

            WeaponAbilityImpactEffects.RestoreResourcesForTargetCount(profile, activator, IAbilityImpactState.GetActiveAbilityImpactSummary(activator));
            WeaponAbilityImpactEffects.AfterImpact(profile, activator, totalDamage, successfulHitCount, IAbilityImpactState.GetActiveAbilityImpactSummary(activator));
        }

        public StatusEffectCategory ResolveStatusCategories(
            WeaponAbilityProfile profile,
            uint activator,
            bool isHostile,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects)
        {
            return isHostile
                    ? AbilityStatusApplication.GetCombatImpactStatusCategories(
                        WeaponAbilityDamageModifiers.ResolveImpactStatus(profile, activator, statusEffect, duration).StatusEffect,
                        additionalStatusEffects,
                        profile.StatusEffectFactory)
                    : StatusEffectCategory.None;
        }

        private static bool ShouldUseTelegraphedCombatImpact(
            AbilityTargetingShapeType targetingShape,
            WeaponAbilityProfile profile)
        {
            return targetingShape != AbilityTargetingShapeType.None || profile.MaximumAreaTargets > 0;
        }

        private static CombatImpactAreaShape ToCombatImpactAreaShape(AbilityTargetingShapeType targetingShape)
        {
            return targetingShape switch
            {
                AbilityTargetingShapeType.Cone => CombatImpactAreaShape.Cone,
                AbilityTargetingShapeType.Rect => CombatImpactAreaShape.Line,
                _ => CombatImpactAreaShape.Sphere
            };
        }

        private static bool CenterAreaOnActivator(AbilityTargetingFlags targetingFlags)
        {
            return (targetingFlags & AbilityTargetingFlags.OriginOnSelf) == AbilityTargetingFlags.OriginOnSelf;
        }
    }
}
