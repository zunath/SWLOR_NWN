using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class SeveringTossAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SeveringToss1(builder);

            return builder.Build();
        }

        private static void SeveringToss1(AbilityBuilder builder)
        {
            builder.Create(FeatType.SeveringToss1, PerkType.SeveringToss)
                .Name("Severing Toss")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SeveringToss, 60f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Throwing, 32, 15, typeof(HamstringStatusEffect), false);
                    break;
            }
        }
    }
}
