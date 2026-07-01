using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class BackstabAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureCastedTarget(
                builder
                    .Create(FeatType.Backstab1, PerkType.Backstab)
                    .Name("Backstab I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Backstab, 60f)
                    .UsesAnimation(Animation.Backstab)
                    .PlaysSoundOnImpact("cb_sw_blade1"),
                14,
                20,
                3);
            ConfigureCastedTarget(
                builder
                    .Create(FeatType.Backstab2, PerkType.Backstab)
                    .Name("Backstab II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.Backstab, 60f)
                    .UsesAnimation(Animation.Backstab)
                    .PlaysSoundOnImpact("cb_sw_blade1"),
                28,
                40,
                5);
            ConfigureCastedTarget(
                builder
                    .Create(FeatType.Backstab3, PerkType.Backstab)
                    .Name("Backstab III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.Backstab, 60f)
                    .UsesAnimation(Animation.Backstab)
                    .PlaysSoundOnImpact("cb_sw_blade1"),
                42,
                60,
                8);

            return builder.Build();
        }

        private static void ConfigureCastedTarget(
            AbilityBuilder ability,
            int baseDamage,
            int rearDamage,
            int stamina)
        {
            ability.HasActivationDelay(0f)
                .SkillType(SkillType.Vibroknife)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var isBehindTarget = Combat.IsTargetNotFacingAttacker(activator, target);
                    var damage = isBehindTarget
                        ? rearDamage
                        : baseDamage;

                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        SkillType.Vibroknife,
                        damage,
                        isBehindTarget && level == 3 ? 3 : 0,
                        isBehindTarget && level == 3 ? typeof(KnockdownStatusEffect) : null,
                        false);
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }
    }
}
