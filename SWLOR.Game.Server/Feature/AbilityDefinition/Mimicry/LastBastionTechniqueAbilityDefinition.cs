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
                .Name("Last Bastion")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.LastBastion, 30f)
                .RequirementStamina(10)
                .IsCastedAbility()
                .MimicryTechnique(FeatType.LastBastion, 47, 3)
                .MimicryUtility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    // Allies get a shield that absorbs 30 damage (temporary HP) for 30 seconds.
                    foreach (var ally in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 8.0f))
                    {
                        TemporaryHitPointEffects.ApplyFlat(ally, "LAST_BASTION", 30, 30f);
                    }

                    // Nearby enemies generate +25% enmity toward the caster for the duration.
                    foreach (var enemy in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(activator), 8.0f, 10))
                    {
                        StatusEffect.ApplyStatusEffect(activator, enemy, new LastBastionStatusEffect(), 30f);
                    }
                });

            return _builder.Build();
        }
    }
}
