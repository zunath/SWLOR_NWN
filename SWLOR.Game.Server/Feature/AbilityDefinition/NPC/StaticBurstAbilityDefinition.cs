using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class StaticBurstAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.StaticBurst,
                "Static Burst",
                InnateAbilityProfile.Devices,
                RecastGroup.StaticBurst,
                1.4f,
                20f,
                6,
                14,
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
