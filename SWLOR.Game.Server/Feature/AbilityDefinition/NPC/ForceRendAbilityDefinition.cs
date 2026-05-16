using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class ForceRendAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.ForceRend,
                "Force Rend",
                InnateAbilityProfile.Force,
                RecastGroup.ForceRend,
                1.2f,
                16f,
                5,
                18,
                15,
                typeof(ForceErosionStatusEffect),
                CombatDamageType.Force,
                ResistanceType.Disruption,
                VisualEffect.Vfx_Imp_Negative_Energy,
                maxRange: 8f);

            return _builder.Build();
        }
    }
}
