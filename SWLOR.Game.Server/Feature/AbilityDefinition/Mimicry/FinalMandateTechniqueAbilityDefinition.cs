using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class FinalMandateTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.FinalMandateTechnique,
                "Final Mandate",
                Animation.FireForgetTaunt,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.4f,
                30f,
                10,
                28,
                14,
                typeof(SuppressionStatusEffect),
                CombatImpactAreaShape.Line,
                8f,
                2.5f,
                CombatDamageType.Sonic,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Magical_Vision,
                VisualEffect.Vfx_Fnf_Howl_War_Cry,
                maxRange: 8f)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.FinalMandate, 4, 3)
                .HasTargetingLine(
                    Spell.FinalMandateTechnique,
                    8f,
                    2.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
