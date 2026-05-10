using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class SoulStormAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SoulStorm(builder);

            return builder.Build();
        }

        private static void SoulStorm(AbilityBuilder builder)
        {
            builder.Create(FeatType.SoulStorm1, PerkType.SoulStorm)
                .Name("Soul Storm")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulStorm, 300f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    SacrificeHitPoints(activator, 40, 10);
                    ApplyStatusToNearbyParty(activator, typeof(SoulStormStatusEffect), 30f, true);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Mind), activator);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(18);
        }
    }
}
