using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class CrushingBlowAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CrushingBlow1(builder);

            return builder.Build();
        }

        private static void CrushingBlow1(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrushingBlow1, PerkType.CrushingBlow)
                .Name("Crushing Blow")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.CrushingBlow, 120f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 20, 16, typeof(CrushingBlowStatusEffect), false);
                    break;
            }
        }
    }
}
