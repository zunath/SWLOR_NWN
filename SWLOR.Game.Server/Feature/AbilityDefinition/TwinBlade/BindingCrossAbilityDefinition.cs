using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class BindingCrossAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BindingCross1(builder);
            BindingCross2(builder);

            return builder.Build();
        }

        private static void BindingCross1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BindingCross1, PerkType.BindingCross)
                .Name("Binding Cross I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.BindingCross, 60f)
                .RequiresTarget()
                .HasImpactAction(BindingCross1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void BindingCross2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BindingCross2, PerkType.BindingCross)
                .Name("Binding Cross II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.BindingCross, 60f)
                .RequiresTarget()
                .HasImpactAction(BindingCross2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void BindingCross1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwinBlade, 10, 12, typeof(HamstringStatusEffect), false);
        }

        private static void BindingCross2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwinBlade, 18, 20, typeof(HamstringStatusEffect), false, additionalStatusEffects: new[] { typeof(ExposedStatusEffect) });
        }
    }
}
