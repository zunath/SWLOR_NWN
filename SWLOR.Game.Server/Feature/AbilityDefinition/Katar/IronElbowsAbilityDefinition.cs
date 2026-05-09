using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class IronElbowsAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            IronElbows1(builder);

            return builder.Build();
        }

        private static void IronElbows1(AbilityBuilder builder)
        {
            builder.Create(FeatType.IronElbows1, PerkType.IronElbows)
                .Name("Iron Elbows")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.IronElbows, 60f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 15, 0, 0, SavingThrow.Will, null, true);
                    break;
            }
        }
    }
}
