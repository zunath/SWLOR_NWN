using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class RimePounceAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.RimePounce,
                "Rime Pounce",
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.RimePounce,
                1.0f,
                15f,
                5,
                15,
                8,
                typeof(FreezingStatusEffect),
                CombatDamageType.Ice,
                ResistanceType.Ice,
                VisualEffect.Vfx_Com_Hit_Frost,
                maxRange: 8f,
                enmityBonus: 75);

            return _builder.Build();
        }
    }
}
