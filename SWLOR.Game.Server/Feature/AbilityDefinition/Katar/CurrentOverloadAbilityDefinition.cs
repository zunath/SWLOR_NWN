using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class CurrentOverloadAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CurrentOverload1(builder);

            return builder.Build();
        }

        private static void CurrentOverload1(AbilityBuilder builder)
        {
            builder.Create(FeatType.CurrentOverload1, PerkType.CurrentOverload)
                .Name("Current Overload")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.CurrentOverload, 90f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 35, 3, 18, SavingThrow.Reflex, typeof(StunnedStatusEffect), false);
                    break;
            }
        }
    }
}
