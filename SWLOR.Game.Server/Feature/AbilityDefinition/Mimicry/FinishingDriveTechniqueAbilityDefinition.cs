using System;
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
    public class FinishingDriveTechniqueAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private const float MomentumDurationSeconds = 30f;

        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            _builder
                .Create(FeatType.FinishingDriveTechnique, PerkType.CombatAnalyzer)
                .Name("Finishing Drive")
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.FinishingDrive, 10f)
                .MimicryTechnique(FeatType.FinishingDrive, 48, 3)
                .MimicryUtility()
                .HasActivationDelay(0f)
                .RequirementStamina(10)
                .IsCastedAbility()
                .BreaksStealth()
                .HasImpactAction((activator, target, level, location) =>
                {
                    // Stacking momentum: each cast adds a stack (up to the cap) and refreshes the
                    // duration. The status effect's magnitude is stacks * potency-per-stack.
                    var existing = StatusEffect.GetStatusEffect(activator, typeof(FinishingDriveMomentumStatusEffect)) as FinishingDriveMomentumStatusEffect;
                    var stacks = Math.Min(FinishingDriveMomentumStatusEffect.MaxStacks, (existing?.Stacks ?? 0) + 1);

                    if (existing != null)
                        StatusEffect.RemoveStatusEffect(activator, typeof(FinishingDriveMomentumStatusEffect), activator, false);

                    StatusEffect.ApplyStatusEffect(activator, activator, new FinishingDriveMomentumStatusEffect(stacks), MomentumDurationSeconds);
                });

            return _builder.Build();
        }
    }
}
