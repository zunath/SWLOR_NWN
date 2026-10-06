using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
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
    public class LastBastionTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            _builder
                .Create(FeatType.LastBastionTechnique, PerkType.CombatAnalyzer)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_LastBastionTechnique)
                .Name("Last Bastion")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.LastBastion, 30f)
                .RequirementStamina(10)
                .IsCastedAbility()
                .UsesImmediateAuthoredAnimation()
                .MimicryTechnique(FeatType.LastBastion, 47, 3)
                .MimicryUtility()
                .RemoveSourceOwnedStatusEffectOnPerkRefund(typeof(LastBastionBarrierStatusEffect))
                .RemoveSourceOwnedStatusEffectOnPerkRefund(typeof(LastBastionStatusEffect))
                .HasImpactAction((activator, target, level, location) =>
                {
                    // Allies get a shield that absorbs 30 damage (temporary HP) for 30 seconds.
                    foreach (var ally in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 8.0f))
                    {
                        if (StatusEffect.ApplyStatusEffect(activator, ally, new LastBastionBarrierStatusEffect(), 30f))
                            Ability.PlaySuccessfulImpactVisualEffect(activator, ally);
                    }

                    // Nearby enemies generate +25% enmity toward the caster for the duration.
                    foreach (var enemy in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(activator), 8.0f, 0))
                    {
                        if (StatusEffect.ApplyStatusEffect(activator, enemy, new LastBastionStatusEffect(), 30f))
                            Ability.PlaySuccessfulImpactVisualEffect(activator, enemy);
                    }
                });

            return _builder.Build();
        }
    }
}
