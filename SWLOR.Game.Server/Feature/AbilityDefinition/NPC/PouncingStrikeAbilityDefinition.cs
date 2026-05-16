using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class PouncingStrikeAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.PouncingStrike,
                "Pouncing Strike",
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.PouncingStrike,
                1.2f,
                18f,
                5,
                16,
                3,
                typeof(KnockdownStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Mobility,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                maxRange: 6f);

            return _builder.Build();
        }
    }
}
