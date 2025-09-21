using System.Collections.Generic;
using SWLOR.Game.Server.Service;


using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class FurorAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public FurorAbilityDefinition(ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Furor();

            return _builder.Build();
        }

        private void Furor()
        {
            _builder.Create(FeatType.Furor, PerkType.Furor)
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
