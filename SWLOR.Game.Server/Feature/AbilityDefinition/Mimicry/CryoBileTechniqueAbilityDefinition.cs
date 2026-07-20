using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class CryoBileTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.CryoBileTechnique,
                "Cryo Bile",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.CryoBile,
                1.6f,
                30f,
                10,
                0,
                6,
                typeof(FreezingStatusEffect),
                CombatImpactAreaShape.Cone,
                8f,
                5f,
                CombatDamageType.Ice,
                ResistanceType.Ice,
                VisualEffect.Vfx_Imp_Frost_L,
                VisualEffect.Vfx_Fnf_Icestorm,
                maxRange: 8f,
                enmityBonus: 100,
                additionalStatusEffects: new[] { typeof(ImmobilizedStatusEffect) })
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .MimicryTechnique(FeatType.CryoBile, 40, 3)
                .HasTargetingCone(
                    Spell.CryoBileTechnique,
                    8f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
