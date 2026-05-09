using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class HamperingBarrageAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            HamperingBarrage1(builder);

            return builder.Build();
        }

        private static void HamperingBarrage1(AbilityBuilder builder)
        {
            builder.Create(FeatType.HamperingBarrage1, PerkType.HamperingBarrage)
                .Name("Hampering Barrage")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.HamperingBarrage, 60f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(14);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Spear, 30, 12, 16, SavingThrow.Reflex, typeof(DisorientedStatusEffect), CombatImpactAreaShape.Cone, 0.25f, 5f, 5f);
                    break;
            }
        }
    }
}
