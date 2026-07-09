using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class LockstepCrushTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.LockstepCrushTechnique,
                "Lockstep Crush Technique",
                Animation.ShieldWall,
                InnateAbilityProfile.Mimicry,
                RecastGroup.Capstone,
                1.0f,
                26f,
                6,
                21,
                8,
                typeof(KnockdownStatusEffect),
                CombatImpactAreaShape.Cone,
                5f,
                5f,
                CombatDamageType.Physical,
                ResistanceType.Mobility,
                VisualEffect.Vfx_Fnf_Screen_Bump,
                VisualEffect.Vfx_Fnf_Screen_Shake,
                maxRange: 5f)
                .SkillType(SkillType.Mimicry)
                .Level(4)
                .MimicryTechnique(FeatType.LockstepCrush, 4, 3)
                .HasTargetingCone(
                    Spell.LockstepCrushTechnique,
                    5f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
