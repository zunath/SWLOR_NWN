using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class DarkShockAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.DarkShock,
                "Dark Shock",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Force,
                RecastGroup.DarkShock,
                1.5f,
                24f,
                7,
                18,
                12,
                typeof(ForceSuppressionStatusEffect),
                CombatImpactAreaShape.Sphere,
                4.5f,
                0f,
                CombatDamageType.Force,
                ResistanceType.Disruption,
                VisualEffect.Vfx_Imp_Negative_Energy,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Evil,
                maxRange: 8f);

            return _builder.Build();
        }
    }
}
