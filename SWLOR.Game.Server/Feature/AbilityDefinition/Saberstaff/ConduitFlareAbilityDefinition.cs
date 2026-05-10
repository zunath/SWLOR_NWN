using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class ConduitFlareAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConduitFlare1(builder);

            return builder.Build();
        }

        private static void ConduitFlare1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ConduitFlare1, PerkType.ConduitFlare)
                .Name("Conduit Flare")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ConduitFlare, 90f)
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
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Saberstaff, 20, 8, typeof(ForceDisruptionStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f, centerOnActivator: true);
                    break;
            }
        }
    }
}
