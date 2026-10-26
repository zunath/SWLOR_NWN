using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public abstract class TwinPistolAbilityDefinitionBase : WeaponActiveAbilityDefinitionBase
    {
        protected static void ConfigurePairAttack(
            AbilityBuilder builder, FeatType feat, PerkType perk, string name, int level,
            RecastGroup recastGroup, float cooldown, TwinPistolAbilitySpec spec, float activationDelay = 0f,
            Spell targetingSpell = Spell.Invalid, AnimationClip channelAnimation = null)
        {
            builder.Create(feat, perk)
                .Name(name)
                .Level(level)
                .SkillType(SkillType.TwinPistol)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasActivationDelay(activationDelay)
                .HasRecastDelay(recastGroup, cooldown)
                .HasMaxRange(spec.MaxRange)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Com_Blood_Spark_Small)
                .HasCustomValidation((actor, target, _, location) => TwinPistolAbility.Validate(actor, target, location, spec))
                .HasActivationAction((actor, target, _, location) => TwinPistolAbility.Begin(actor, target, location, spec))
                .HasImpactAction((actor, target, _, location) => TwinPistolAbility.Complete(actor, target, location, spec))
                .RequirementStamina(spec.StaminaCost);

            if (spec.MomentumPowerPerCharge > 0)
                builder.HasWeaponAbilityFlowDamagePerCharge(spec.MomentumPowerPerCharge);
            if (!spec.SpendsMomentum)
                builder.KeepsWeaponAbilityFlow();

            if (spec.VolleyCount > 1)
                builder.IsChanneledAbility(TwinPistolAbility.InterruptChannel, suppressAutoAttacks: true)
                    .HasFixedActivationDelay(spec.ChannelSeconds)
                    .UsesAnimation(channelAnimation);
            else
                builder.UsesAuthoredAnimationAtImpact();

            if (spec.Movement == TwinPistolMovementType.FreeDestination)
            {
                // A click past the dash's reach still rushes the full distance toward it.
                builder.IsAreaAbility()
                    .ClampsLocationTargetToMaxRange()
                    .HasTargetingLine(targetingSpell, spec.MaxMovementMeters, spec.LineWidthMeters,
                        AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);
            }
            else
                builder.IsSingleTargetAbility().RequiresTarget();
        }

        protected static void ConfigurePairStance(
            AbilityBuilder builder, FeatType feat, PerkType perk, string name, RecastGroup recastGroup, Type status)
        {
            builder.Create(feat, perk)
                .Name(name)
                .Level(1)
                .SkillType(SkillType.TwinPistol)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .HasRecastDelay(recastGroup, 30f)
                .RequirementStamina(0)
                .UsesImmediateAuthoredAnimation()
                .UsesAnimation(Animation.CastOutAnimation)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Imp_Haste);
            ConfigureToggle(builder, status);
        }
    }
}
