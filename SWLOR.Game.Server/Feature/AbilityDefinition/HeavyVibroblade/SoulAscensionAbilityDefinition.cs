using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public sealed class SoulAscensionAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public const int HitPointCostPercent = 10;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder
                .Create(FeatType.SoulAscensionBurst, PerkType.SoulAscension)
                .Name("Soul Ascension")
                .Level(1)
                .SkillType(SkillType.HeavyVibroblade)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingConjure1)
                .UsesImmediateAuthoredAnimation()
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .HasAIHitPointCostPercent(_ => HitPointCostPercent)
                .HasCustomValidation((activator, target, level, location) =>
                    CanPayHitPointCost(GetCurrentHitPoints(activator), GetMaxHitPoints(activator))
                        ? string.Empty
                        : "You need more than 10% of your maximum HP to use Soul Ascension.")
                .RemoveSourceOwnedStatusEffectOnPerkRefund(typeof(SoulAscensionBurstStatusEffect))
                .IsCastedAbility()
                .BreaksStealth()
                .HasImpactAction((activator, target, level, location) =>
                {
                    if (StatusEffect.ApplyStatusEffect(activator, activator, new SoulAscensionBurstStatusEffect(),
                            CapstoneAbility.ActiveDurationSeconds))
                        SacrificeHitPoints(activator, HitPointCostPercent);
                });

            return builder.Build();
        }

        public static bool CanPayHitPointCost(int currentHitPoints, int maximumHitPoints)
        {
            return maximumHitPoints > 0 && currentHitPoints > GameMath.PercentOf(maximumHitPoints, HitPointCostPercent);
        }
    }
}
