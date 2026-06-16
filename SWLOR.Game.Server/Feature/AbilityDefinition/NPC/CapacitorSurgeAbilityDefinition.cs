using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class CapacitorSurgeAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.CapacitorSurge,
                "Capacitor Surge",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Devices,
                RecastGroup.CapacitorSurge,
                1.2f,
                20f,
                5,
                13,
                9,
                typeof(ShockStatusEffect),
                CombatImpactAreaShape.Sphere,
                4f,
                0f,
                CombatDamageType.Electrical,
                ResistanceType.Electrical,
                VisualEffect.Vfx_Imp_Lightning_M,
                VisualEffect.Vfx_Fnf_Electric_Explosion,
                centerOnActivator: true);

            return _builder.Build();
        }
    }
}
