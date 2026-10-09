using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public sealed class BloodFrenzyAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder
                .Create(FeatType.BloodFrenzyBurst, PerkType.BloodFrenzy)
                .Name("Blood Frenzy")
                .Level(1)
                .SkillType(SkillType.Vibroblade)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingConjure1)
                .UsesImmediateAuthoredAnimation()
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .RequirementStamina(CapstoneAbility.StaminaCost)
                .RemoveSourceOwnedStatusEffectOnPerkRefund(typeof(BloodFrenzyStatusEffect))
                .IsCastedAbility()
                .BreaksStealth()
                .HasImpactAction((activator, target, level, location) =>
                    StatusEffect.ApplyStatusEffect(activator, activator, new BloodFrenzyStatusEffect(),
                        CapstoneAbility.ActiveDurationSeconds));

            return builder.Build();
        }
    }
}
