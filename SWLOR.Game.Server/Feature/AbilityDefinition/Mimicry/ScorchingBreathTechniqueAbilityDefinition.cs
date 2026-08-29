using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class ScorchingBreathTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.ScorchingBreathTechnique,
                "Scorching Breath",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.ScorchingBreath,
                1.5f,
                30f,
                10,
                40,
                30,
                typeof(BurnStatusEffect),
                CombatImpactAreaShape.Cone,
                8f,
                5f,
                CombatDamageType.Fire,
                ResistanceType.Fire,
                VisualEffect.Vfx_Com_Hit_Fire,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Fire,
                additionalStatusEffects: new[] { typeof(WeakenedStatusEffect) })
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.ScorchingBreath, 50, 3)
                .HasTargetingCone(
                    Spell.ScorchingBreathTechnique,
                    8f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf | AbilityTargetingFlags.BackOffsetOrigin);

            return _builder.Build();
        }
    }
}
