using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class WardenOrderTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            _builder
                .Create(FeatType.WardenOrderTechnique, PerkType.CombatAnalyzer)
                .Name("Warden Order")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.WardenOrder, 30f)
                .RequirementStamina(10)
                .IsCastedAbility()
                .MimicryTechnique(FeatType.WardenOrder, 49, 3)
                .MimicryUtility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    foreach (var ally in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5.5f))
                    {
                        var amount = GameMath.PercentOf(GetMaxHitPoints(ally), 15);
                        amount = Stat.ApplyOutgoingAbilityHealingAdjustment(activator, amount);
                        ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), ally);
                    }
                });

            return _builder.Build();
        }
    }
}
