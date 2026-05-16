using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class RakingClawsAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.RakingClaws,
                "Raking Claws",
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.RakingClaws,
                1f,
                11f,
                3,
                14,
                12,
                typeof(HamstringStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Mobility,
                VisualEffect.Vfx_Com_Chunk_Red_Small);

            return _builder.Build();
        }
    }
}
