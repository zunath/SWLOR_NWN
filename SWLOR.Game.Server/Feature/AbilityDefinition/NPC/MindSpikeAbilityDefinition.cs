using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class MindSpikeAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.MindSpike,
                "Mind Spike",
                InnateAbilityProfile.Force,
                RecastGroup.MindSpike,
                1.1f,
                18f,
                4,
                12,
                12,
                typeof(TerrifiedStatusEffect),
                CombatDamageType.Force,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Fear_S,
                maxRange: 8f);

            return _builder.Build();
        }
    }
}
