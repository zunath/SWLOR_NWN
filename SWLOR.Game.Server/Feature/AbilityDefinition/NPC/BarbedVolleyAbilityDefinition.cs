using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class BarbedVolleyAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.BarbedVolley,
                "Barbed Volley",
                Animation.ThrowGrenade,
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.BarbedVolley,
                1.4f,
                17f,
                5,
                15,
                12,
                typeof(BleedStatusEffect),
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
