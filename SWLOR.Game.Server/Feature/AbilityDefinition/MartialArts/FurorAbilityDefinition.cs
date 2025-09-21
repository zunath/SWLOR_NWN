using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class FurorAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private readonly CombatPoint _combatPoint;
        private readonly IEnmityService _enmityService;

        public FurorAbilityDefinition(CombatPoint combatPoint, IEnmityService enmityService)
        {
            _combatPoint = combatPoint;
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
                    _combatPoint.AddCombatPointToAllTagged(activator, SkillType.MartialArts, 3);
                });
        }
    }
}
