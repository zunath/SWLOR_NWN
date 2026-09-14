using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class TacticalMarkAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.TacticalMark,
                "Tactical Mark",
                Animation.PointForward,
                InnateAbilityProfile.Rifle,
                RecastGroup.TacticalMark,
                0.9f,
                20f,
                4,
                10,
                15,
                typeof(ExposeWeakPointStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Imp_Magical_Vision,
                maxRange: 12f);

            return _builder.Build();
        }
    }
}
