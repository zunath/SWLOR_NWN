using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class SavageRoarAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.SavageRoar,
                "Savage Roar",
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.SavageRoar,
                1f,
                21f,
                4,
                0,
                14,
                typeof(WeakenedStatusEffect),
                CombatImpactAreaShape.Sphere,
                6f,
                0f,
                CombatDamageType.Sonic,
                ResistanceType.Mind,
                VisualEffect.Vfx_Fnf_Howl_War_Cry,
                VisualEffect.Vfx_Fnf_Howl_War_Cry,
                centerOnActivator: true);

            return _builder.Build();
        }
    }
}
