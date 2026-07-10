using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class MercilessAngleTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.MercilessAngleTechnique,
                "Merciless Angle Technique",
                Animation.DoubleThrust,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.0f,
                26f,
                10,
                28,
                8,
                typeof(HemorrhageStatusEffect),
                CombatImpactAreaShape.Cone,
                5f,
                5f,
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Chunk_Red_Medium,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                maxRange: 5f)
                .SkillType(SkillType.Mimicry)
                .Level(4)
                .CombatImpactDamageAbility(AbilityType.Social)
                .MimicryTechnique(FeatType.MercilessAngle, 4, 3)
                .HasTargetingCone(
                    Spell.MercilessAngleTechnique,
                    5f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
