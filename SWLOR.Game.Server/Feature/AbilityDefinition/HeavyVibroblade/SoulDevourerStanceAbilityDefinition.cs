using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class SoulDevourerStanceAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SoulDevourerStance(builder);

            return builder.Build();
        }

        private static void SoulDevourerStance(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SoulDevourerStance1, PerkType.SoulDevourerStance)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_SoulDevourer)
                .UsesImmediateAuthoredAnimation()
                .Name("Soul Devourer Stance")
                .Level(1)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.SoulDevourerStance, 30f)
                .HasActivationAction((activator, target, level, targetLocation) => ToggleSelfStatus(activator, typeof(SoulDevourerStanceStatusEffect)))
                .RemoveStatusEffectOnPerkRefund(typeof(SoulDevourerStanceStatusEffect))
                .HasImpactAction((activator, target, level, targetLocation) => ApplySelfStatus(activator, typeof(SoulDevourerStanceStatusEffect)))
                .SkillType(SkillType.HeavyVibroblade)
                .IsCastedAbility()
                .BreaksStealth();
        }
    }
}
