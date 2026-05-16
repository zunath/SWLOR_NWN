using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class ShrapnelBurstAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.ShrapnelBurst,
                "Shrapnel Burst",
                InnateAbilityProfile.Devices,
                RecastGroup.ShrapnelBurst,
                1.4f,
                20f,
                6,
                18,
                12,
                typeof(SunderStatusEffect),
                CombatImpactAreaShape.Cone,
                8f,
                5f,
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Imp_Wallspike,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                maxRange: 8f);

            return _builder.Build();
        }
    }
}
