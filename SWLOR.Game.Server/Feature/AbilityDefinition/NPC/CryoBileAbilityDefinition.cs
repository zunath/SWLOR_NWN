using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class CryoBileAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.CryoBile,
                "Cryo Bile",
                Animation.CastOutAnimation,
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.CryoBile,
                1.6f,
                24f,
                8,
                18,
                12,
                typeof(FreezingStatusEffect),
                CombatImpactAreaShape.Cone,
                8f,
                5f,
                CombatDamageType.Ice,
                ResistanceType.Ice,
                VisualEffect.Vfx_Imp_Frost_L,
                VisualEffect.Vfx_Fnf_Icestorm,
                maxRange: 8f,
                enmityBonus: 100);

            return _builder.Build();
        }
    }
}
