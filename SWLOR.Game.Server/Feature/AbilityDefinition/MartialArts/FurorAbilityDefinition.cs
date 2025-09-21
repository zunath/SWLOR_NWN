using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityServicex;


using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
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
