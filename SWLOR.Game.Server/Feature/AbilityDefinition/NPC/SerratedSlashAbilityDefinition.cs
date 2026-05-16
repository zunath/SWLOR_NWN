using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class SerratedSlashAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.SerratedSlash,
                "Serrated Slash",
                InnateAbilityProfile.Vibroblade,
                RecastGroup.SerratedSlash,
                1.1f,
                15f,
                4,
                20,
                12,
                typeof(HemorrhageStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Chunk_Red_Medium);

            return _builder.Build();
        }
    }
}
