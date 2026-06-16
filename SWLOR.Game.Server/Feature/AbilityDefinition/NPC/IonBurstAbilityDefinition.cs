using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class IonBurstAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.IonBurst,
                "Ion Burst",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Devices,
                RecastGroup.IonBurst,
                1.3f,
                18f,
                5,
                14,
                12,
                typeof(DisorientedStatusEffect),
                CombatImpactAreaShape.Cone,
                8f,
                5f,
                CombatDamageType.Electrical,
                ResistanceType.Electrical,
                VisualEffect.Vfx_Imp_Lightning_S,
                VisualEffect.Vfx_Fnf_Storm,
                maxRange: 8f);

            return _builder.Build();
        }
    }
}
