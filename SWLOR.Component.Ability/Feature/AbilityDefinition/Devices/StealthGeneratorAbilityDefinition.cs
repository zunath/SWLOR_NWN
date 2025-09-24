using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Combat.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public class StealthGeneratorAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public StealthGeneratorAbilityDefinition(ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            StealthGenerator1(builder);
            StealthGenerator2(builder);
            StealthGenerator3(builder);

            return builder.Build();
        }

        [ScriptHandler(ScriptName.OnHarvesterUsed)]
        [ScriptHandler<OnPlayerDamaged>]
        public static void ClearInvisibility()
        {
            RemoveEffect(OBJECT_SELF, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility);
        }

        private void StealthGenerator1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.StealthGenerator1, PerkType.StealthGenerator)
                .Name("Stealth Generator I")
                .Level(1)
                .HasRecastDelay(RecastGroup.StealthGenerator, 360f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.Crouch)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectInvisibility(InvisibilityType.Normal), activator, 30f);

                    _enmityService.ModifyEnmityOnAll(activator, 450);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
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
                .UsesAnimation(Animation.Crouch)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectInvisibility(InvisibilityType.Normal), activator, 60f);

                    _enmityService.ModifyEnmityOnAll(activator, 750);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
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
                .UsesAnimation(Animation.Crouch)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectInvisibility(InvisibilityType.Normal), activator, 120f);

                    _enmityService.ModifyEnmityOnAll(activator, 950);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }


    }
}
