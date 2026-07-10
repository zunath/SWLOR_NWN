using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class ApexCollapseTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.ApexCollapseTechnique,
                "Apex Collapse Technique",
                Animation.DoubleThrust,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.4f,
                38f,
                10,
                28,
                14,
                typeof(HemorrhageStatusEffect),
                CombatImpactAreaShape.Line,
                8f,
                2.5f,
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Blood_Crt_Red,
                VisualEffect.Vfx_Fnf_Screen_Shake,
                maxRange: 8f)
                .SkillType(SkillType.Mimicry)
                .Level(4)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.ApexCollapse, 4, 3)
                .HasTargetingLine(
                    Spell.ApexCollapseTechnique,
                    8f,
                    2.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
