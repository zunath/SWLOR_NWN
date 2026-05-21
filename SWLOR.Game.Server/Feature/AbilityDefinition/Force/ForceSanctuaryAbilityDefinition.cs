using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ForceSanctuaryAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceSanctuary1(builder);

            return builder.Build();
        }

        private static void ForceSanctuary1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceSanctuary1, PerkType.ForceSanctuary)
                .Name("Force Sanctuary")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ForceSanctuary, 90f)
                .SkillType(SkillType.Force)
                .IsAreaAbility()
                .HasImpactAction(ForceSanctuary1ImpactAction)
                .HasTargetingSphere(
                    Spell.ForceSanctuary1,
                    4f,
                    AbilityTargetingFlags.HelpsAllies)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void ForceSanctuary1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);

            AbilityAreaEffects.ScheduleFriendlyZoneStatus(
                activator,
                location,
                4f,
                18f,
                typeof(ForceSanctuary1StatusEffect),
                VisualEffect.Vfx_Imp_Holy_Aid);

            AbilityAreaEffects.ScheduleFriendlyZoneHealing(
                activator,
                location,
                4f,
                18f,
                2f,
                null,
                VisualEffect.Vfx_Imp_Healing_M);
        }
    }
}
