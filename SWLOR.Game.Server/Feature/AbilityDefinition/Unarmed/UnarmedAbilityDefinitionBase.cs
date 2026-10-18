using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed
{
    public abstract class UnarmedAbilityDefinitionBase : WeaponActiveAbilityDefinitionBase
    {
        protected static void ConfigureStrike(
            AbilityBuilder builder,
            MeleeTechniqueDetail technique,
            int power,
            int stamina,
            float activationDelay = 0f,
            int statusDuration = 0,
            Func<IStatusEffect> statusFactory = null)
        {
            // Heavy strikes use the existing cast window for their windup and strike.
            // Starting the long gesture at impact lets resumed autoattacks erase it.
            if (activationDelay > 0f)
                builder.UsesImmediateAuthoredAnimation();
            else
                builder.UsesAuthoredAnimationAtImpact();

            builder.MeleeTechnique(technique)
                .SkillType(SkillType.Unarmed)
                .HasActivationDelay(activationDelay)
                .IsCastedAbility()
                .IsHostileAbility()
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasMaxRange(7f)
                .BreaksStealth()
                .UsesImpactAnimation(Animation.DoubleStrike)
                .HasCustomValidation((activator, target, level, location) => ValidateGrip(activator))
                .HasActivationAction((activator, target, level, location) =>
                {
                    Combat.StoreAbilityActivationIdleBonuses(activator, SkillType.Unarmed);
                    return true;
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    try
                    {
                        for (var impact = 0; impact < technique.ImpactCount; impact++)
                        {
                            var impactTarget = MeleeTechnique.GetImpactTarget(activator, target, impact);
                            var conditionalPower = MeleeTechnique.GetConditionalPower(activator, impactTarget, impact);
                            Ability.ApplyCombatImpact(
                                activator, impactTarget, location, SkillType.Unarmed,
                                power + conditionalPower, statusDuration, null, false,
                                statusEffectFactory: statusFactory,
                                statusResistanceType: ResistanceType.Trauma,
                                playImpactAnimation: impact == 0 && (activationDelay <= 0f || !GetIsPC(activator)));
                        }
                    }
                    finally
                    {
                        Combat.ClearAbilityActivationIdleBonuses(activator);
                    }
                })
                .RequirementStamina(stamina);
        }

        protected static void ConfigureStance(AbilityBuilder builder, Type statusType)
        {
            ConfigureToggle(builder, statusType);
            builder.SkillType(SkillType.Unarmed)
                .UsesImmediateAuthoredAnimation();
        }

        private static string ValidateGrip(uint creature) => EquipmentPredicates.HasUnarmedGrip(creature)
            ? string.Empty
            : "Equip a Power Glove and leave your off hand available.";
    }
}
