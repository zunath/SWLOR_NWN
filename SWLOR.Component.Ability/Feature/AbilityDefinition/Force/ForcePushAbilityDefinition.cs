using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForcePushAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ForcePushAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForcePush1(builder);
            ForcePush2(builder);
            ForcePush3(builder);
            ForcePush4(builder);

            return builder.Build();
        }
        
        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            const float BaseDuration = 2f;
            int dc;

            switch (level)
            {
                default:
                case 1:
                    dc = 8;
                    break;
                case 2:
                    dc = 12;
                    break;
                case 3:
                    dc = 14;
                    break;
                case 4:
                    dc = 16;
                    break;
            }

            var willpowerBonus = 0.5f * GetAbilityModifier(AbilityType.Willpower, activator);

            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Will, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
            var duration = BaseDuration + willpowerBonus;

            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, duration);

                AbilityService.ApplyTemporaryImmunity(target, duration, ImmunityType.Knockdown);
            }
            else if (checkResult == SavingThrowResultType.Success)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectSlow(), target, duration);
            }

            EnmityService.ModifyEnmityOnAll(activator, level * 150);

            CombatPointService.AddCombatPoint(activator, target, SkillType.Force, 3);
        }

        private void ForcePush1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush1, PerkType.ForcePush)
                .Name("Force Push I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ForcePush, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(1)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }

        private void ForcePush2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush2, PerkType.ForcePush)
                .Name("Force Push II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ForcePush, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(2)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }

        private void ForcePush3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush3, PerkType.ForcePush)
                .Name("Force Push III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ForcePush, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }

        private void ForcePush4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush4, PerkType.ForcePush)
                .Name("Force Push IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.ForcePush, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(4)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }
    }
}
