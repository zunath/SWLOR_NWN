using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.MartialArts
{
    public class FurorAbilityDefinition: IAbilityListDefinition
    {
        private readonly IEnmityService _enmityService;
        private readonly ICombatPointService _combatPointService;

        public FurorAbilityDefinition(IEnmityService enmityService, ICombatPointService combatPointService)
        {
            _enmityService = enmityService;
            _combatPointService = combatPointService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Furor(builder);

            return builder.Build();
        }

        private void Furor(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Furor, PerkType.Furor)
                .Name("Furor")
                .HasRecastDelay(RecastGroup.Furor, 180f)
                .HasActivationDelay(3f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectModifyAttacks(1), activator, 60f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Negative_Energy), activator);

                    _enmityService.ModifyEnmityOnAll(activator, 450);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.MartialArts, 3);
                });
        }
    }
}
