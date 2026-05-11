using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class SoulStrikeAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SoulStrike1(builder);
            SoulStrike2(builder);
            SoulStrike3(builder);

            return builder.Build();
        }

        private static void SoulStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SoulStrike1, PerkType.SoulStrike)
                .Name("Soul Strike I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulStrike, 45f)
                .HasImpactAction((activator, target, level, targetLocation) => SoulStrikeImpact(activator, target, targetLocation, 15, 25))
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void SoulStrike2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SoulStrike2, PerkType.SoulStrike)
                .Name("Soul Strike II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulStrike, 45f)
                .HasImpactAction((activator, target, level, targetLocation) => SoulStrikeImpact(activator, target, targetLocation, 30, 40))
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void SoulStrike3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SoulStrike3, PerkType.SoulStrike)
                .Name("Soul Strike III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulStrike, 45f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var percent = Math.Min(90, 60 + Math.Max(0, GetAbilityModifier(AbilityType.Might, activator)));
                    SoulStrikeImpact(activator, target, targetLocation, 45, percent);
                })
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(15);
        }
    }
}
