using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class FinishingDriveTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.FinishingDriveTechnique,
                "Finishing Drive Technique",
                Animation.Whirlwind,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.4f,
                30f,
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
                .CombatImpactDamageAbility(AbilityType.Agility)
                .MimicryTechnique(FeatType.FinishingDrive, 4, 3)
                .HasTargetingLine(
                    Spell.FinishingDriveTechnique,
                    8f,
                    2.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
