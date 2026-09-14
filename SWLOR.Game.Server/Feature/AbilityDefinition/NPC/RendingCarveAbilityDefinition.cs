using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class RendingCarveAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.RendingCarve,
                "Rending Carve",
                Animation.CrossCut,
                InnateAbilityProfile.Vibroblade,
                RecastGroup.RendingCarve,
                1.0f,
                18f,
                5,
                24,
                14,
                typeof(HemorrhageStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Chunk_Red_Medium);

            return _builder.Build();
        }
    }
}
