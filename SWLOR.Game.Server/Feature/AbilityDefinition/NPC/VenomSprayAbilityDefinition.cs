using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class VenomSprayAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.VenomSpray,
                "Venom Spray",
                Animation.CastOutAnimation,
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.VenomSpray,
                1.4f,
                18f,
                5,
                14,
                12,
                typeof(PoisonStatusEffect),
                CombatImpactAreaShape.Cone,
                8f,
                5f,
                CombatDamageType.Poison,
                ResistanceType.Poison,
                VisualEffect.Vfx_Imp_Poison_S,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Acid,
                maxRange: 8f);

            return _builder.Build();
        }
    }
}
