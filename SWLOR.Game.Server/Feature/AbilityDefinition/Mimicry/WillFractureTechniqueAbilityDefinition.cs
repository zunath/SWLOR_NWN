using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class WillFractureTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.WillFractureTechnique,
                "Will Fracture Technique",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.0f,
                26f,
                10,
                28,
                8,
                typeof(FoggyMindStatusEffect),
                CombatImpactAreaShape.Cone,
                5f,
                5f,
                CombatDamageType.Force,
                ResistanceType.Mind,
                VisualEffect.Vfx_Imp_Fear_S,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Mind,
                maxRange: 5f)
                .SkillType(SkillType.Mimicry)
                .Level(4)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .MimicryTechnique(FeatType.WillFracture, 4, 3)
                .HasTargetingCone(
                    Spell.WillFractureTechnique,
                    5f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
