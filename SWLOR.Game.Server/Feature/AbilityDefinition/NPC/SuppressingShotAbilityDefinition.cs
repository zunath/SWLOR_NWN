using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class SuppressingShotAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.SuppressingShot,
                "Suppressing Shot",
                Animation.PointPistol,
                InnateAbilityProfile.Rifle,
                RecastGroup.SuppressingShot,
                1.2f,
                17f,
                4,
                12,
                4,
                typeof(DazedStatusEffect),
                CombatImpactAreaShape.Line,
                10f,
                2.5f,
                CombatDamageType.Physical,
                ResistanceType.Mind,
                VisualEffect.Vfx_Com_Special_Blue_Red,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                maxRange: 10f);

            return _builder.Build();
        }
    }
}
