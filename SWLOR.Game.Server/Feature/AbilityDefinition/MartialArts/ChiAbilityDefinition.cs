using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AbilityServicex;


using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class ChiAbilityDefinition: IAbilityListDefinition
    {
        private readonly IEnmityService _enmityService;
        private readonly ICombatPointService _combatPointService;

        public ChiAbilityDefinition(IEnmityService enmityService, ICombatPointService combatPointService)
        {
            _enmityService = enmityService;
            _combatPointService = combatPointService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Chi1(builder);
            Chi2(builder);
            Chi3(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, int baseRecovery)
        {
            var bonusRecovery = GetAbilityModifier(AbilityType.Willpower, activator) * 8;
            var recovery = baseRecovery + bonusRecovery;

            ApplyEffectToObject(DurationType.Instant, EffectHeal(recovery), activator);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_G), activator);

            _enmityService.ModifyEnmityOnAll(activator, 300 + recovery + 10);
            _combatPointService.AddCombatPointToAllTagged(activator, SkillType.MartialArts, 3);
        }

        private void Chi1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Chi1, PerkType.Chi)
                .Name("Chi I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Chi, 180f)
                .HasActivationDelay(1.0f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, 45);
                });
        }

        private void Chi2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Chi2, PerkType.Chi)
                .Name("Chi II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Chi, 180f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, 115);
                });
        }

        private void Chi3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Chi3, PerkType.Chi)
                .Name("Chi III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Chi, 180f)
                .HasActivationDelay(3.0f)
                .RequirementStamina(10)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, 170);
                });
        }
    }
}
