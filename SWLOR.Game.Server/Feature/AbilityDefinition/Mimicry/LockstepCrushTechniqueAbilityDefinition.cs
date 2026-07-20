using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service;
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
                "Lockstep Crush",
                Animation.ShieldWall,
                InnateAbilityProfile.Mimicry,
                RecastGroup.LockstepCrush,
                1.0f,
                30f,
                10,
                40,
                6,
                typeof(KnockdownStatusEffect),
                CombatImpactAreaShape.Cone,
                5f,
                5f,
                CombatDamageType.Physical,
                ResistanceType.Mobility,
                VisualEffect.Vfx_Com_Chunk_Red_Medium,
                VisualEffect.Vfx_Fnf_Screen_Shake,
                maxRange: 5f,
                afterSuccessfulHit: (activator, target) =>
                    StatusEffect.ApplyStatusEffect<SunderStatusEffect>(activator, target, 30f, CombatDamageType.Physical))
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Agility)
                .MimicryTechnique(FeatType.LockstepCrush, 43, 3)
                .HasTargetingCone(
                    Spell.LockstepCrushTechnique,
                    5f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
