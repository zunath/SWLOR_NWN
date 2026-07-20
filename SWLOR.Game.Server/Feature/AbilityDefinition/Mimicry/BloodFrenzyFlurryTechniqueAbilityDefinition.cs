using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class BloodFrenzyFlurryTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.BloodFrenzyFlurryTechnique,
                "Blood Frenzy Flurry",
                Animation.Whirlwind,
                InnateAbilityProfile.Mimicry,
                RecastGroup.BloodFrenzyFlurry,
                0.8f,
                24f,
                8,
                28,
                30,
                typeof(BleedStatusEffect),
                CombatImpactAreaShape.Cone,
                5f,
                5f,
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Blood_Spark_Medium,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                maxRange: 5f)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Agility)
                .MimicryTechnique(FeatType.BloodFrenzyFlurry, 43, 3)
                .HasTargetingCone(
                    Spell.BloodFrenzyFlurryTechnique,
                    5f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
