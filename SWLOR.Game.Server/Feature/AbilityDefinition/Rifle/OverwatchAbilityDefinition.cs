using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class OverwatchAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Overwatch1(builder);

            return builder.Build();
        }

        private static void Overwatch1(AbilityBuilder builder)
        {
            builder.Create(FeatType.Overwatch1, PerkType.Overwatch)
                .Name("Overwatch")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.Overwatch, 120f)
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
                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        SkillType.Rifle,
                        20,
                        12,
                        typeof(FoggyMindStatusEffect),
                        false,
                        statusEffectFactory: () => new FoggyMindStatusEffect(2));
                    break;
            }
        }
    }
}
