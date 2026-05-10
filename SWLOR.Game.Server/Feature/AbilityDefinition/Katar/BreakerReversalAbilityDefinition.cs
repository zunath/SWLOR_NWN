using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class BreakerReversalAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BreakerReversal1(builder);

            return builder.Build();
        }

        private static void BreakerReversal1(AbilityBuilder builder)
        {
            builder.Create(FeatType.BreakerReversal1, PerkType.BreakerReversal)
                .Name("Breaker Reversal")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.BreakerReversal, 60f)
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 35, 12, typeof(ExposedStatusEffect), false);
                    break;
            }
        }
    }
}
