using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class BrutalBashAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.BrutalBash,
                "Brutal Bash",
                InnateAbilityProfile.Staff,
                RecastGroup.BrutalBash,
                1f,
                16f,
                4,
                12,
                3,
                typeof(KnockdownStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Mobility,
                VisualEffect.Vfx_Fnf_Screen_Bump);

            return _builder.Build();
        }
    }
}
