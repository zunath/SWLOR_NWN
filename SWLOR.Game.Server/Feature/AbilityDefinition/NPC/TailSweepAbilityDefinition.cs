using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class TailSweepAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.TailSweep,
                "Tail Sweep",
                InnateAbilityProfile.CreaturePhysical,
                RecastGroup.TailSweep,
                1.4f,
                18f,
                5,
                12,
                4,
                typeof(DazedStatusEffect),
                CombatImpactAreaShape.Sphere,
                4.5f,
                0f,
                CombatDamageType.Physical,
                ResistanceType.Mind,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                centerOnActivator: true);

            return _builder.Build();
        }
    }
}
