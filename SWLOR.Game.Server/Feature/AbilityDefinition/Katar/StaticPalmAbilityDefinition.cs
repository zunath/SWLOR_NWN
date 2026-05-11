using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class StaticPalmAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            StaticPalm1(builder);
            StaticPalm2(builder);
            StaticPalm3(builder);

            return builder.Build();
        }

        private static void StaticPalm1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.StaticPalm1, PerkType.StaticPalm)
                .Name("Static Palm I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.StaticPalm, 30f)
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void StaticPalm2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.StaticPalm2, PerkType.StaticPalm)
                .Name("Static Palm II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.StaticPalm, 30f)
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void StaticPalm3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.StaticPalm3, PerkType.StaticPalm)
                .Name("Static Palm III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.StaticPalm, 30f)
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 8, 8, typeof(DisorientedStatusEffect), false, damageType: CombatDamageType.Electrical);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 18, 12, typeof(DisorientedStatusEffect), false, damageType: CombatDamageType.Electrical);
                    break;
                case 3:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 28, 15, typeof(DisorientedStatusEffect), false, new[] { typeof(DazedStatusEffect) }, damageType: CombatDamageType.Electrical);
                    break;
            }
        }
    }
}
