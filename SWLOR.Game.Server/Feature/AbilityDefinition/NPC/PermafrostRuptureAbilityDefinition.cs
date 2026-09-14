using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class PermafrostRuptureAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.PermafrostRupture,
                "Permafrost Rupture",
                Animation.DoubleThrust,
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.PermafrostRupture,
                2.0f,
                26f,
                7,
                16,
                8,
                typeof(FreezingStatusEffect),
                CombatImpactAreaShape.Sphere,
                5.5f,
                0f,
                CombatDamageType.Ice,
                ResistanceType.Ice,
                VisualEffect.Vfx_Imp_Pulse_Cold,
                VisualEffect.Vfx_Fnf_Icestorm,
                centerOnActivator: true);

            return _builder.Build();
        }
    }
}
