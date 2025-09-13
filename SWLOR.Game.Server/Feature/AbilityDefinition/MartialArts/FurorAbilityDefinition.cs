using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class FurorAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

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

                    Enmity.ModifyEnmityOnAll(activator, 450);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.MartialArts, 3);
                });
        }
    }
}
