using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class EarthshatterAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Earthshatter1(builder);

            return builder.Build();
        }

        private static void Earthshatter1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Earthshatter1, PerkType.Earthshatter)
                .Name("Earthshatter")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.Earthshatter, 90f)
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
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 20, 12, typeof(ForceDisruptionStatusEffect), CombatImpactAreaShape.Line, 0.25f, 8f, 2.5f);
                    break;
            }
        }
    }
}
