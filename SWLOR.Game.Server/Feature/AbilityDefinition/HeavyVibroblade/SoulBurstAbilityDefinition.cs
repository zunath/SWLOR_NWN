using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class SoulBurstAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SoulBurst1(builder);

            return builder.Build();
        }

        private static void SoulBurst1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SoulBurst1, PerkType.SoulBurst)
                .Name("Soul Burst")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulBurst, 180f)
                .HasImpactAction(SoulBurst1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void SoulBurst1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 35, 0, null, CombatImpactAreaShape.Cone, 0.25f, 5f, 5f);
        }
    }
}
