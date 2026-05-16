using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class FrostSpitAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.FrostSpit,
                "Frost Spit",
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.FrostSpit,
                1.2f,
                16f,
                4,
                14,
                12,
                typeof(FreezingStatusEffect),
                CombatDamageType.Ice,
                ResistanceType.Ice,
                VisualEffect.Vfx_Imp_Frost_S,
                maxRange: 8f);

            return _builder.Build();
        }
    }
}
