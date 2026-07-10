using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class BrutalBashTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.BrutalBashTechnique,
                "Brutal Bash Technique",
                Animation.DoubleThrust,
                InnateAbilityProfile.Mimicry,
                RecastGroup.BrutalBash,
                1.0f,
                15f,
                5,
                16,
                3,
                typeof(KnockdownStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Mobility,
                VisualEffect.Vfx_Fnf_Screen_Bump)
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.BrutalBash, 2, 2);

            return _builder.Build();
        }
    }
}
