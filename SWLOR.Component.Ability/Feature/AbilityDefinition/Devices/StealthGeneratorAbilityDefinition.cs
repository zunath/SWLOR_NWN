using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public class StealthGeneratorAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public StealthGeneratorAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            StealthGenerator1(builder);
            StealthGenerator2(builder);
            StealthGenerator3(builder);

            return builder.Build();
        }

        public static void ClearInvisibility()
        {
            RemoveEffect(OBJECT_SELF, EffectScriptType.Invisibility, EffectScriptType.ImprovedInvisibility);
        }

        private void StealthGenerator1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.StealthGenerator1, PerkType.StealthGenerator)
                .Name("Stealth Generator I")
                .Level(1)
                .HasRecastDelay(RecastGroup.StealthGenerator, 360f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .UsesAnimation(AnimationType.Crouch)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectInvisibility(InvisibilityType.Normal), activator, 30f);

                    EnmityService.ModifyEnmityOnAll(activator, 450);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void StealthGenerator2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.StealthGenerator2, PerkType.StealthGenerator)
                .Name("Stealth Generator II")
                .Level(2)
                .HasRecastDelay(RecastGroup.StealthGenerator, 360f)
                .HasActivationDelay(2f)
                .RequirementStamina(6)
                .UsesAnimation(AnimationType.Crouch)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectInvisibility(InvisibilityType.Normal), activator, 60f);

                    EnmityService.ModifyEnmityOnAll(activator, 750);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void StealthGenerator3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.StealthGenerator3, PerkType.StealthGenerator)
                .Name("Stealth Generator III")
                .Level(3)
                .HasRecastDelay(RecastGroup.StealthGenerator, 360f)
                .HasActivationDelay(2f)
                .RequirementStamina(8)
                .UsesAnimation(AnimationType.Crouch)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectInvisibility(InvisibilityType.Normal), activator, 120f);

                    EnmityService.ModifyEnmityOnAll(activator, 950);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }


    }
}
