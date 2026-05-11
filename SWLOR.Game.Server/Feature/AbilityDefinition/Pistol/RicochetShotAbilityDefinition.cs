using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class RicochetShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RicochetShot1(builder);

            return builder.Build();
        }

        private static void RicochetShot1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RicochetShot1, PerkType.RicochetShot)
                .Name("Ricochet Shot")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.RicochetShot, 60f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Pistol, 12, 6, typeof(BlindStatusEffect), false);
                    break;
            }
        }
    }
}
