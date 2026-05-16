using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class DreadWaveAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.DreadWave,
                "Dread Wave",
                InnateAbilityProfile.Force,
                RecastGroup.DreadWave,
                1.2f,
                24f,
                6,
                12,
                12,
                typeof(TerrifiedStatusEffect),
                CombatImpactAreaShape.Sphere,
                6f,
                0f,
                CombatDamageType.Sonic,
                ResistanceType.Mind,
                VisualEffect.Vfx_Fnf_Howl_Mind,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Mind,
                centerOnActivator: true);

            return _builder.Build();
        }
    }
}
