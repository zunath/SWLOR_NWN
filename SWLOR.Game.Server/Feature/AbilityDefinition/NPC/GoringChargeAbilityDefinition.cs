using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class GoringChargeAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.GoringCharge,
                "Goring Charge",
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.GoringCharge,
                1.5f,
                22f,
                6,
                22,
                10,
                typeof(BleedStatusEffect),
                CombatImpactAreaShape.Line,
                8f,
                2.5f,
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Chunk_Red_Medium,
                VisualEffect.Vfx_Fnf_Screen_Shake,
                maxRange: 8f);

            return _builder.Build();
        }
    }
}
