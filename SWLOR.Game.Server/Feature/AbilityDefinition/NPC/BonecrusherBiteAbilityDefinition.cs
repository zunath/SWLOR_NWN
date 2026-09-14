using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class BonecrusherBiteAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.BonecrusherBite,
                "Bonecrusher Bite",
                Animation.DoubleStrike,
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.BonecrusherBite,
                1.3f,
                16f,
                5,
                20,
                14,
                typeof(SunderStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Chunk_Red_Medium);

            return _builder.Build();
        }
    }
}
