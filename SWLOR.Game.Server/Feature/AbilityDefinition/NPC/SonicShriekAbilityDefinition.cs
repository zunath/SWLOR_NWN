using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class SonicShriekAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.SonicShriek,
                "Sonic Shriek",
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.SonicShriek,
                1.3f,
                19f,
                5,
                16,
                12,
                typeof(DisorientedStatusEffect),
                CombatImpactAreaShape.Cone,
                8f,
                5f,
                CombatDamageType.Sonic,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Sonic,
                VisualEffect.Vfx_Fnf_Sound_Burst,
                maxRange: 8f);

            return _builder.Build();
        }
    }
}
