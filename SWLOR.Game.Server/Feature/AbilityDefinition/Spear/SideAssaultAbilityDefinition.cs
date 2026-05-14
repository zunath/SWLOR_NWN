using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class SideAssaultAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSideAssault(
                builder
                    .Create(FeatType.SideAssault1, PerkType.SideAssault)
                    .Name("Side Assault I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.SideAssault, 12f),
                12,
                16,
                6);
            ConfigureSideAssault(
                builder
                    .Create(FeatType.SideAssault2, PerkType.SideAssault)
                    .Name("Side Assault II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.SideAssault, 12f),
                25,
                35,
                12);
            ConfigureSideAssault(
                builder
                    .Create(FeatType.SideAssault3, PerkType.SideAssault)
                    .Name("Side Assault III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.SideAssault, 12f),
                35,
                50,
                18);

            return builder.Build();
        }

        private static void ConfigureSideAssault(
            AbilityBuilder ability,
            int baseDamage,
            int sideDamage,
            int stamina)
        {
            ability.HasActivationDelay(0f)
                .SkillType(SkillType.Spear)
                .IsSingleTargetAbility()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var damage = Combat.IsAttackerBesideTarget(activator, target)
                        ? sideDamage
                        : baseDamage;

                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        SkillType.Spear,
                        damage,
                        0,
                        null,
                        false);
                })
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }
    }
}
