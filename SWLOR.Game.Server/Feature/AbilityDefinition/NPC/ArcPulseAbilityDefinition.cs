using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class ArcPulseAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.ArcPulse,
                "Arc Pulse",
                InnateAbilityProfile.Devices,
                RecastGroup.ArcPulse,
                1.3f,
                20f,
                6,
                16,
                12,
                typeof(ShockStatusEffect),
                CombatImpactAreaShape.Sphere,
                4.5f,
                0f,
                CombatDamageType.Electrical,
                ResistanceType.Electrical,
                VisualEffect.Vfx_Imp_Lightning_S,
                VisualEffect.Vfx_Fnf_Storm,
                centerOnActivator: true);

            return _builder.Build();
        }
    }
}
