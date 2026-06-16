using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class StaticWebAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.StaticWeb,
                "Static Web",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Devices,
                RecastGroup.StaticWeb,
                1.4f,
                22f,
                6,
                14,
                10,
                typeof(ShockStatusEffect),
                CombatImpactAreaShape.Sphere,
                4.5f,
                0f,
                CombatDamageType.Electrical,
                ResistanceType.Electrical,
                VisualEffect.Vfx_Imp_Head_Electricity,
                VisualEffect.Dur_Web,
                centerOnActivator: true);

            return _builder.Build();
        }
    }
}
